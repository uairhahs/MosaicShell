;========================================
;Top Part
;========================================

[MeasureComputerName]
Measure=Plugin
Plugin=SysInfo
SysInfoType=COMPUTER_NAME
UpdateDivider=-1

[MeasureTime]
Measure=Time
Format=%#gVar.2412#:%M:%S
UpdateDivider=1
Disabled=#CustomFormat#
[MeasureAP]
Measure=Time
Format=%p
UpdateDivider=1
Disabled=#CustomFormat#
[MeasureDate]
Measure=Time
Format=%a, %#b %#d
[MeasureOSBits]
Measure=Plugin
Plugin=SysInfo
SysInfoType=OS_BITS
UpdateDivider=-1
[MeasureMonitors]
Measure=Plugin
Plugin=SysInfo
SysInfoType=NUM_MONITORS
UpdateDivider=-1
[MeasureUsername]
Measure=Plugin
Plugin=SysInfo
SysInfoType=USER_NAME
UpdateDivider=-1

[MeasureProductName]
Measure=Plugin
Plugin=SysInfo
SysInfoType=OS_VERSION
[MeasureVersion]
Measure=Registry
RegHKey=HKEY_LOCAL_MACHINE
RegKey=SOFTWARE\Microsoft\Windows NT\CurrentVersion
RegValue=DisplayVersion
UpdateDivider=-1
[MeasureCurrentBuild]
Measure=Registry
RegHKey=HKEY_LOCAL_MACHINE
RegKey=SOFTWARE\Microsoft\Windows NT\CurrentVersion
RegValue=CurrentBuild
UpdateDivider=-1
[MeasureUBR]
Measure=Registry
RegHKey=HKEY_LOCAL_MACHINE
RegKey=SOFTWARE\Microsoft\Windows NT\CurrentVersion
RegValue=UBR
UpdateDivider=-1

;========================================
;RAM
;========================================

[MeasurePhysMemTotal]
Measure=PhysicalMemory
Total=1
UpdateDivider=-1
[MeasurePhysMemUsed]
Measure=PhysicalMemory
UpdateDivider=5
[MeasureRAM]
Measure=Calc
Formula=(MeasurePhysMemUsed / MeasurePhysMemTotal) * 100
UpdateDivider=5
[RAMTextActual]
Measure=Calc
Formula=MeasureRAM/5

;========================================
;CPU
;========================================

[MeasureCPU]
Measure=Plugin
Plugin=UsageMonitor
Alias=CPU
Index=0
RegExpSubstitute=1
Substitute="^0$":"0.01"
[CPUTextActual]
Measure=Calc
Formula=MeasureCPU/5

[CPUPhyC]
Measure=Plugin
Plugin=RunCommand
Parameter=wmic CPU Get NumberOfCores
State=Hide
OutputType=ANSI
RegExpSubstitute=1
Substitute="#CRLF#":"", "NumberOfCores":"", " ":""
UpdateDivider=-1

[CPUVirC]
Measure=Plugin
Plugin=RunCommand
Parameter=wmic CPU Get NumberOfLogicalProcessors
State=Hide
OutputType=ANSI
RegExpSubstitute=1
Substitute="#CRLF#":"", "NumberOfLogicalProcessors":"", " ":""
UpdateDivider=-1

;========================================
;GPU
;========================================

[MeasureGPU]
Measure=Plugin
Plugin=UsageMonitor
Alias=GPU
Index=0
Blacklist=dwm
DynamicVariables=1
[GPUTextActual]
Measure=Calc
Formula=MeasureGPU/5

;========================================
;Bin
;========================================

[RecycleBinFiles]
Measure=RecycleManager
RecycleType=COUNT
Drives=ALL
UpdateDivider=60
Group=Bin
Substitute=".0":""
[RecycleBinSize]
Measure=RecycleManager
RecycleType=SIZE
Drives=ALL
UpdateDivider=60
Group=Bin

;========================================
;Uptime
;========================================

[MeasureUptime]
Measure=Uptime
Format="%4!i! days, %3!i! hours, %2!i! minutes %1!i! seconds"

;========================================
;Internet
;========================================

[InternetType]
Measure=Plugin
Plugin=SysInfo
SysInfoType=ADAPTER_TYPE
SysInfoData=Best
UpdateDivider=-1
IfCondition=InternetType=6
IfTrueAction=[!SetVariable NetType Ethernet][!UpdateMeasure InternetStatus]
IfCondition2=InternetType=71
IfTrueAction2=[!SetVariable NetType Wifi][!UpdateMeasure InternetStatus]
[InternetName]
Measure=PLUGIN
Plugin=WifiStatus
WiFiInfoType=SSID
Substitute="-1":""
[InternetStatus]
Measure=PLUGIN
Plugin=SysInfo
SysInfoType=INTERNET_CONNECTIVITY
Substitute="-1.0":"Offline","1.0":"Connected"
DynamicVariables=1
IfCondition=InternetStatus = -1
IfTrueAction=[!SetOption Internet Text "%6#CRLF#%1#CRLF#Click here to connect to the internet"][!UpdateMeter Internet][!Redraw]
IfFalseAction=[!SetOption Internet Text "%6#CRLF#%1 %2-%3 Dl %4b | Ul %5b"][!UpdateMeter Internet][!Redraw]
[mAUTH]
Measure=PLUGIN
Plugin=WifiStatus
WiFiInfoType=AUTH
Substitute="-1":""
[InternetDownloadSpeed]
Measure=NetIn
Interface=Best
MinValue=0
AverageSize=5
[InternetUploadSpeed]
Measure=NetOut
Interface=Best
MinValue=0
AverageSize=5

;========================================
;Names
;========================================

[MeasureGPUName]
Measure=Plugin
Plugin=RunCommand
Program=powershell.exe
Parameter=Get-WmiObject Win32_VideoController | Select-Object -Expand Name
OutputType=UTF8
Substitute="#CRLF#":""
[MeasureCPUname]
Measure=Plugin
Plugin=RunCommand
Program=powershell.exe
Parameter=Get-WmiObject win32_Processor | Select-Object -Expand Name
OutputType=UTF8
Substitute="#CRLF#":""
[MeasureMotherboardname]
Measure=Plugin
Plugin=RunCommand
Program=powershell.exe
Parameter=Get-WmiObject Win32_BaseBoard | Select-Object -Expand Product
OutputType=UTF8
Substitute="#CRLF#":""

; ================================================
; Drives
; ================================================
