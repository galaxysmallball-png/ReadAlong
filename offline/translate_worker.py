"""Local Argos en->zh model runner; stdio only, no web service or text logs."""
import json
import os
from pathlib import Path
import re
import socket
import sys
import time


def deny_network(*args, **kwargs):
    raise RuntimeError("Networking is disabled in the offline translation worker")


class OfflineSocket(socket.socket):
    def __init__(self, *args, **kwargs):
        deny_network()


socket.socket = OfflineSocket
socket.create_connection = deny_network

import ctranslate2
import sentencepiece


OFFLINE_ROOT = Path(__file__).resolve().parent
MODEL = OFFLINE_ROOT / "models" / "translate-en_zh-1_9"
_translator = None
_tokenizer = None


def load_model():
    global _translator, _tokenizer
    if _translator is None:
        metadata = json.loads((MODEL / "metadata.json").read_text(encoding="utf-8"))
        if metadata.get("from_code") != "en" or metadata.get("to_code") != "zh":
            raise ValueError("Expected the English-to-Chinese model")
        _translator = ctranslate2.Translator(
            str(MODEL / "model"), device="cpu", compute_type="int8",
            inter_threads=1, intra_threads=min(4, max(1, os.cpu_count() or 1)),
        )
        # Load bytes so the native tokenizer never has to open a Windows
        # filename containing Chinese or other non-ASCII characters.
        _tokenizer = sentencepiece.SentencePieceProcessor(
            model_proto=(MODEL / "sentencepiece.model").read_bytes()
        )


def translate(text):
    if not isinstance(text, str) or not text.strip() or len(text) > 4000:
        raise ValueError("Expected 1-4000 characters")
    load_model()
    tokens = _tokenizer.encode(text.strip(), out_type=str)
    if len(tokens) > 512:
        raise ValueError("Please split long text into shorter sentences")
    result = _translator.translate_batch(
        [tokens], beam_size=2, length_penalty=0.2,
        replace_unknowns=True, max_decoding_length=512,
    )
    output = _tokenizer.decode(result[0].hypotheses[0]).replace("\u2581", " ").strip()
    output = re.sub(r"\s+([，。！？；：])", r"\1", output)
    return re.sub(r"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])", "", output)


def check():
    start = time.perf_counter()
    text = translate("Please select a folder to save your exported files.")
    assert re.search(r"[\u4e00-\u9fff]", text), "Model did not return Chinese"
    try:
        socket.create_connection(("example.com", 443), timeout=1)
    except RuntimeError:
        blocked = True
    else:
        raise AssertionError("Offline worker must refuse network connections")
    return {"ready": True, "translation": text, "networkBlocked": blocked,
            "model": "Argos en-zh 1.9", "elapsedMs": round((time.perf_counter()-start)*1000)}


def main():
    if "--check" in sys.argv:
        print(json.dumps(check(), ensure_ascii=True), flush=True)
        return
    load_model()
    for raw in sys.stdin.buffer:
        try:
            request = json.loads(raw.decode("utf-8"))
            if request.get("command") == "health":
                reply = {"ready": True, "model": "Argos en-zh 1.9"}
            else:
                reply = {"translation": translate(request.get("text"))}
        except Exception:
            reply = {"error": "离线模型处理失败，请重试或将文本拆成短句。"}
        print(json.dumps(reply, ensure_ascii=True), flush=True)


if __name__ == "__main__":
    main()
