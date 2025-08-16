# Radio Program App | 簡単ラジオ番組表

## 概要

このアプリケーションは、[radiko.jp](https://radiko.jp/) からラジオの放送局情報と番組表を取得して表示するためのWPFデスクトップアプリケーションです。

## 主な機能

- **放送局一覧の表示:** 全国のラジオ放送局を地域別に一覧で表示します。
- **地域別フィルタリング:** ドロップダウンリストから地域を選択し、その地域の放送局に絞り込んで表示することができます。
- **番組情報の取得:** 現在放送中の番組を取得し、番組名、放送局名、番組内容、バナーを表示します。

## 開発環境

- .NET 8
- C#
- Windows Presentation Foundation (WPF)

## プロジェクトの実行方法

### 前提条件

- Visual Studio 2022 以降
- .NET 8 SDK

### 手順

1.  このリポジトリをクローンします。
    ```sh
    git clone <repository-url>
    ```
2.  Visual Studio で `RadioProgramApp.sln` ファイルを開きます。
3.  Visual Studio のメニューから `ビルド` > `ソリューションのビルド` を選択して、プロジェクトをビルドします。
4.  `F5` キーを押すか、ツールバーの `開始` ボタンをクリックしてアプリケーションを実行します。

## プロジェクト構成

```
RadioProgramApp/
│
├── Models/               # データモデル
│   ├── Station.cs        # 放送局情報を表すモデル
│   ├── ProgramInfo.cs    # 番組情報を表すモデル
│   └── ParseRadikoStation.cs # radiko.jpからXMLを解析するクラス
│
├── ViewModels/           # ViewModel
│   ├── StationsViewModel.cs # 放送局一覧ビューのロジック
│   └── ...
│
├── Views/                # View (XAML)
│   ├── StationsView.xaml   # 放送局一覧を表示するUI
│   └── ...
│
├── App.xaml              # アプリケーションのエントリポイント
└── MainWindow.xaml       # メインウィンドウ
```
