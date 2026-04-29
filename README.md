# VR Depth Placement Research

[![Unity Version](https://img.shields.io/badge/Unity-6000.4.4f1-blue.svg)](https://unity.com/)
[![Target Platform](https://img.shields.io/badge/Platform-Meta%20Quest%202%20%2F%203-lightgrey.svg)]()

早稲田大学 情報理工学科 プロジェクト研究用リポジトリ
テーマ：**3D空間における奥行き配置のインターフェース比較検証**

## 概要 (Overview)
本プロジェクトは、VR空間における3D物体の「奥行き配置」における最適な操作インターフェースを検証するための研究用アプリケーションです。

評価用タスクとして、連続的な物理演算と空間把握が要求される「3D物理パズル（スイカゲーム風のタスク）」を実装しています。被験者は異なる3つの操作モードでタスクを実行し、配置精度、所要時間、およびユーザーの認知負荷を比較・評価します。

## 実装されている操作モード (Interaction Modes)
本アプリケーションでは、コントローラー（Meta Quest）を用いた以下の3つの配置手法を比較検証します。

1. **レーザーポインター方式 (Laser Pointer Mode)**
   * コントローラーから放射されるRaycastを用いて、遠隔から落下地点を指示する従来の手法。
2. **直接把持方式 (Direct Grab Mode)**
   * 物体を直接「手」で掴み、配置したい3D空間上の座標まで直接移動させてから離す手法。
3. **放物線投擲方式 (Parabolic Throw Mode)**
   * コントローラーの加速度とスイング軌道を用いて物体を「投げる」手法。軌道予測線（Trajectory Line）を視覚的補助として表示。

## システム要件 (Requirements)
* **Game Engine:** Unity 6000.4.4f1 (Unity 6.4)
* **VR SDK:** Meta XR All-in-One SDK / XR Interaction Toolkit (XRI)
* **Hardware:** Meta Quest 2 / Meta Quest 3 / Meta Quest Pro
* **OS:** Windows 11 / macOS (Apple Silicon M3 動作確認済み)

## 開発環境のセットアップ (Setup Instructions)
1. 本リポジトリをクローンします。
   ```bash
   git clone [https://github.com/aiouku/ProjectResearch.git](https://github.com/aiouku/ProjectResearch.git)