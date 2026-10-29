# UI 设计与动效参考

已安装并用于本应用迭代的 skills：

| Skill | 用途 |
| --- | --- |
| [Interface Design](https://github.com/Dammyjay93/interface-design) | 应用布局、层级和控件一致性 |
| [UI UX Pro Max](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill) | 排版、配色、可读性和交互规范 |
| [apple-design](https://github.com/emilkowalski/skills/tree/main/skills/apple-design) | 可中断弹簧、对称收缩展开路径、玻璃层次和减少动画 |

当前主界面以逐句对照阅读为核心，上方直接开始或暂停，次要功能放在侧栏。采用薄荷青与淡紫玻璃层、Source Serif 4 原生英文斜体、霞鹜文楷屏幕版中文手写字体。窗口与阅读字号可分别缩放。

识别扫光参考 [Shimmer](https://github.com/facebookarchive/Shimmer) 和 [flutter_shimmer](https://github.com/hnvn/flutter_shimmer) 的加载提示表现，在 WPF 独立实现。没有复制项目代码或引入移动端依赖。扫光只随识别或翻译活动出现，等待或暂停时停止；减少动画时显示静态光带。

收缩与展开用本应用的内存快照绘制漏斗效果，支持途中反向。收缩后保留伴读，小浮窗同步显示忙碌状态。动效与截图均使用隔离的示例数据，不包含个人屏幕。