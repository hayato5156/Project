# Project
Test

## 執行測試

此專案的單元測試需要安裝 .NET 8 SDK。如果系統尚未安裝 `dotnet` 指令，可依以下步驟於本機安裝：

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
export PATH="$HOME/.dotnet:$PATH"
```

完成安裝後，於專案根目錄執行下列指令即可跑測試：

```bash
dotnet test ECommercePlatform/ECommercePlatform.sln
```
