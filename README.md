# WatchDog_Sub
![](https://hackaday.com/wp-content/uploads/2021/10/Watchdog.jpg?w=600&h=450 "WatchDog")

# Getting Started
## 系統需求
* 作業系統 `Microsoft Windows XP, 7, 8/8.1, 10`
* Microsoft .NET Framework 2.0 `↑`

## 安裝說明
* 需要將 `WatchDog_main.exe` 和 `WatchDog_Sub.exe` 放置同一目錄資料夾
* 預設第一次若無設定檔，啟動會自動生成`PROCESS.xml`，須關閉程式後重啟

## 設定檔說明
*  `WatchDog_main.exe` 和 `WatchDog_Sub.exe` 共用一份設定檔 `PROCESS.xml`
```
Name: 監控的程式名稱 (程式在工作管理員-顯示的名稱)
Path   : 監控的程式路徑 (絕對路徑)
Timer : 預設監控檢查並重啟時間(單位秒)
```
```
需要監控的程式增加在 <PS>\<MainProcess>\ 下，新增下列範例
<ProcessItem>
	<Name>NewWatch</Name>
	<Path>C:\tmp\Watcher\NewWatch.exe</Path>
	<Timer>5</Timer>
</ProcessItem>

```
```
<?xml version="1.0" encoding="utf-8"?>
<PS xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
	<MainProcess>
		<ProcessItem>
			<Name>WatchDog_Sub</Name>
			<Path>C:\tmp\WatchDog_Sub.exe</Path>
			<Timer>2</Timer>
		</ProcessItem>
		<ProcessItem>
			<Name>NewWatch</Name>
			<Path>C:\tmp\Watcher\NewWatch.exe</Path>
			<Timer>5</Timer>
		</ProcessItem>
	</MainProcess>
	<SubProcess>
		<ProcessItem>
			<Name>WatchDog_Main</Name>
			<Path>C:\tmp\WatchDog_Main.exe</Path>
			<Timer>2</Timer>
		</ProcessItem>
	</SubProcess>
</PS>
```
