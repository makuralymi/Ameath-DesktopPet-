# Ameath-DesktopPet

鸣潮爱弥斯飞行雪绒桌宠。

注意⚠️ 本项目由AI辅助完成！

轻量级 WinForms 桌宠应用，支持多状态、随机行为、GIF 动画播放与鼠标交互。

## 功能
- 无边框、透明背景、置顶、不显示任务栏
- GIF 多状态动画：Idle/Wander/Interact/Drag
- 随机行为状态机与优先级控制
- 鼠标拖动、点击响应、托盘菜单
- 可选鼠标穿透、固定位置、滚轮缩放

## 目录
```
Ameath-DesktopPet/
  gif/                 # 素材目录（运行时读取）
  Controllers/
  Core/
  Managers/
  PetForm.cs
  Program.cs
  Ameath-DesktopPet.csproj
```

## 素材命名
所有素材文件名不包含空格，示例：
- `hu.gif`, `nothing.gif`
- `fly.gif`
- `happy.gif`, `happy2.gif`
- `jump.gif`, `jump2.gif`
- `cool.gif`, `cute.gif`

## 构建
```
dotnet restore
dotnet build
```

## 运行
```
dotnet run
```

## 发布（单文件）
```
dotnet publish -c Release
```
输出目录：`bin/Release/net6.0-windows/win-x64/publish/`

发布为框架依赖单文件，目标机器需已安装 .NET 运行时。
请将 `gif` 目录与 `Ameath-DesktopPet.exe` 放在同一目录下。

## 打包（MSIX）
### 方式一：Visual Studio
1. 新建“Windows 应用程序打包项目（MSIX）”。
2. 选择“引用现有项目”，指向 `Ameath-DesktopPet`。
3. 配置包名、版本、图标后发布，生成 `.msix`。

### 方式二：MSIX Packaging Tool
1. 先发布：`dotnet publish -c Release`
2. 使用 MSIX Packaging Tool 选择发布目录生成 `.msix`。

## 备注
- `cool.gif`/`cute.gif` 会自动缩放到与待机基准图相同大小。
- 启动时会后台预热大图资源以减少首次切换卡顿。
