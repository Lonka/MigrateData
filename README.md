# MigrateData

## 主要功能
1. 標準化的轉資料函式，透過設定檔可互轉
2. 可以支援Oracle,MSSQL互轉（可擴充）
3. 可以支援互轉中途Plug In處理其它事
4. 支援時區設定
5. 失敗可以支援重傳（三次）
6. 可支援下列四種型態互轉
    - 批次
    - 同步
    - 復製
    - 更新

## INI設定

### 基本
```INI
[Migrate Setting]
'時區[Default:0]
timeZone=
'dll放置的資料夾[Default:Empty]
folder=
```

### 來源資料庫
```INI
[Source Database]
'資料庫類型[Contain:SqlServer][Default:SqlServer]
type=
'連線字串，啟動後加密[Default:Empty]
connectionString=
```

### 目標資料庫
[Target Database]
'資料庫類型[Contain:SqlServer][Default:SqlServer]
type=
'連線字串，啟動後加密[Default:Empty]
connectionString=

### Log
[Log Setting]
'每次log的最大字元數(byte)[Default:200]
retentionDay=
'過期刪除的天數[Default:30]
maxLogSize=
'存放路徑[Default:.\LogFiles]
path=

### Task
'Task的section一定要包含「Migrate Task」後面可以自訂
[Migrate Task Empty]
'轉檔的形式(大小寫都要對)[Contain:Batch,Sync,Copy,Update][Default:Batch]
taskType=
'來源表
sourceTable=
'目標表
targetTable=
'檢查的欄位，只能輸入一個欄位值(大小寫都要對)[Default:Empty](source)
checkField=
'檢查欄位的型態[Contain:double,datetime][Default:datetime](source)
checkType=
'檢查欄位的初使值[Default:1970/1/1 00:00:00(datetime),0(double)](source)
checkBeginValue=
'檢查時是否要包含等於[Default:false](source)
checkEqual=
'Sync Table所需要的確認key，用','隔開(大小寫都要對)[Default:Empty](source)
syncField=
'來源資料是否需要條件[ex:A=1 and B=2][Default:Empty](source)
condition=
'來源資料是否需要額外欄位[ex:A,B][Default:Empty](source)
extraField=
'轉資料時是否要額外處理，用','隔開[Default:Empty]
plugIn=

'設定欄位對應  Target Field=Source Field(大小寫都要對)(如果來源值要寫死的話，直接寫tempFieldName(value))
A=B



