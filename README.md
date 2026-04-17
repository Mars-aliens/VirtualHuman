# VirtualHuman-RhythmGame

![Unity](https://img.shields.io/badge/Unity-2022.3+-black.svg?style=flat&logo=unity)
![C#](https://img.shields.io/badge/C%23-Language-blue.svg?style=flat&logo=c-sharp)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg?style=flat&logo=windows)

基于 Unity 引擎开发的 3D 交互式节奏游戏。项目集成了人物动画系统、实时音频同步判定以及完整的 UI 结算流程。

## 🚀 快速体验 (Download & Play)

本项目已完成编译发布。如果你只想直接体验游戏，请前往 **[Releases](https://github.com/Mars-aliens/VirtualHuman/releases)** 页面：

1. 下载最新的 `.zip` 压缩包。
2. 解压到本地任意文件夹。
3. 运行 `VirtualHuman.exe` 即可开始游戏。

---

## 🎮 玩法操作 (How to Play)

本项目采用**四键位独立判定系统**，玩家需要根据节奏精准打击：

* **核心键位**：
  * **[A]** 
  * **[D]** 
  * **[←] (左箭头)** 
  * **[→] (右箭头)** 
* **游戏暂停**：在游戏进行中按 **ESC** 键可唤起暂停菜单。
* **结算系统**：游戏结束后自动计算 **Score**、**Max Combo** 并生成 **Rank (S/A/B)**。

---

## 🛠️ 技术亮点 (Technical Features)

* **四轨道并发检测**：利用 C# 的 Input 映射，实现了多键位独立触发逻辑，提升了游戏的打击感与操作维度。
* **精确结算判定**：通过 `DelayedShowResult` 协程优化了 UI 赋值顺序，确保在 `Time.timeScale = 0` 触发前完成最终数据的填入与 Rank 计算。
* **状态机流转**：严谨处理了“开始菜单 -> 游戏运行 -> 暂停/继续 -> 结算面板”的完整逻辑链路。

---

## 📂 项目结构说明

* `Assets/Scripts/`: 游戏逻辑核心 C# 脚本。
* `Assets/Scenes/`: 包含主菜单、核心游戏场景。
* `ProjectSettings/`: Unity 项目环境配置。

---

## 👨‍💻 作者

**(Mars-aliens)**
* Computer Science Major @ Beijing Institute of Technology (BIT)
* GitHub: [Mars-aliens](https://github.com/Mars-aliens)

## 📜 致谢与素材来源 (Credits & Attributions)

本项目在开发过程中使用了以下资源，特此致谢：

* **核心音乐 (BGM)**:
    * **歌曲名称**: 《(You're the) Devil in Disguise》
    * **用途**: 本项目核心节奏关卡曲目。
* **UI 界面素材 (GUI Assets)**:
    * **Silent - Game GUI Asset**: 由 [Prinbles](https://prinbles.itch.io/) 开发。
* **音效素材 (SFX Assets)**:
    * **Universal UI/Menu Soundpack**: 由 [Cyrex Studios](https://cyrex-studios.itch.io/) 开发。
* **模型与动画 (Models & Animations)**:
    * 角色模型与舞蹈动作：源自 [Mixamo (Adobe)](https://www.mixamo.com/)。

> **版权声明**: 本项目所使用的音频（Devil in Disguise）及美术资源版权归原作者/版权方所有。本项目仅作为BIT相关课程学习与个人技术展示使用，不涉及任何商业用途。
