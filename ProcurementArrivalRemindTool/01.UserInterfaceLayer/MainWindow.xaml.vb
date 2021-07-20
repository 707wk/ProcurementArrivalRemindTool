Imports System.Data.SqlClient
Imports System.IO
Imports System.Timers
Imports DingTalk.Api
Imports DingTalk.Api.Request
Imports DingTalk.Api.Response
Imports Microsoft.AppCenter.Analytics
Imports OfficeOpenXml

Class MainWindow

    Private SendTimer As Timer

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        Me.Title = $"{My.Application.Info.Title} V{AppSettingHelper.Instance.ProductVersion}"

        '设置使用方式为个人使用
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial

        Dim tmpAppCenterSparkle As New AppCenterSparkle(AppSettingHelper.AppKey, Me)
        tmpAppCenterSparkle.CheckUpdateAsync()

        StartAutoRun.IsChecked = AppSettingHelper.Instance.StartAutoRun

        SendTimer = New Timer With {
            .Interval = 60 * 1000
        }
        AddHandler SendTimer.Elapsed, AddressOf SendTimerElapsed

        SendTimer.Start()

    End Sub

    ''' <summary>
    ''' 定时处理
    ''' </summary>
    Private Sub SendTimerElapsed(sender As Object, e As ElapsedEventArgs)
        Console.WriteLine("定时处理")

        ' 定时间隔发送
        If (Now - AppSettingHelper.Instance.LastSendDate) < AppSettingHelper.Instance.SearchTimeSpan Then
            Exit Sub
        End If

        ' 清空昨天的发送记录
        If Now.Year <> AppSettingHelper.Instance.LastSendDate.Year AndAlso
            Now.Month <> AppSettingHelper.Instance.LastSendDate.Month AndAlso
            Now.Day <> AppSettingHelper.Instance.LastSendDate.Day Then

            AppSettingHelper.Instance.SendDocumentIDItems.Clear()
            Analytics.TrackEvent("清空昨天的发送记录")
            AppSettingHelper.Instance.Logger.Info("清空昨天的发送记录")

        End If

        AppSettingHelper.Instance.LastSendDate = Now
        AppSettingHelper.SaveToLocaltion()

        Analytics.TrackEvent("自动查找数据")
        AppSettingHelper.Instance.Logger.Info("自动查找数据")

        Me.Dispatcher.Invoke(Sub()
                                 WorkFunction(Nothing, Nothing)
                             End Sub)

    End Sub

    Public Sub Shutdown()

        SendTimer.Stop()
        RemoveHandler SendTimer.Elapsed, AddressOf SendTimerElapsed

        AppSettingHelper.SaveToLocaltion()

        System.Windows.Application.Current.Shutdown()

        End

    End Sub

    Private Sub UpdateInfoMenuItem_Click(sender As Object, e As RoutedEventArgs)

        FileHelper.Open("https://install.appcenter.ms/orgs/hunan-yestech/apps/cai3-gou4-dao4-huo4-ti2-xing3-gong1-ju4/distribution_groups/public")

    End Sub

    Private Sub AboutMenuItem_Click(sender As Object, e As RoutedEventArgs)

        Dim tmpWindow As New AboutWindow With {
          .Owner = Me
        }
        tmpWindow.ShowDialog()

    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)

        e.Cancel = True

        Me.WindowState = WindowState.Minimized

    End Sub

    Private Sub WorkFunction(sender As Object, e As RoutedEventArgs)

        If e IsNot Nothing Then
            Analytics.TrackEvent("手动查找数据")
            AppSettingHelper.Instance.Logger.Info("手动查找数据")
        End If

        Dim tmpWindow As New Wangk.ResourceWPF.BackgroundWork(Me) With {
            .Title = "初始化"
        }

        tmpWindow.Run(Sub(uie)
                          Dim stepCount = 5

#Region "获取今天到货物料列表"
                          uie.Write("获取未结束表单", 0 * 100 / stepCount)

                          AppSettingHelper.Instance.DocumentItems.Clear()

                          Using SqlConn As New SqlConnection(AppSettingHelper.Instance.ERPSqlServerConnStr)
                              SqlConn.Open()

                              Using tmpSqlCommand = SqlConn.CreateCommand
                                  tmpSqlCommand.CommandText = $"select 
PURTD.TD026 as 请购单别,
PURTD.TD027 as 请购单号,
PURTD.TD028 as 请购序号,
PURTA.TA003 as 请购日期,
PURTA.TA012 as 请购人员,
CMSMV.MV002 as 员工姓名,
PURTB.TB009 as 请购数量,
PURTD.TD004 as 品号,
PURTD.TD005 as 品名,
PURTD.TD006 as 规格,
rtrim(CMSMC.MC002)+'('+rtrim(TempPURTH.TH009)+')' as 仓库,
TempPURTH.TH015 as 验收数量,
TempPURTH.TH014 as 验收日期

from
    (select
    *

    -- 进货单单身档
    from PURTH
    -- 审核码
    where TH030='Y'
    -- 验收日期
    and TH014='{Now:yyyyMMdd}') as TempPURTH

-- 采购单单身信息档
left join PURTD
on PURTD.TD001=TempPURTH.TH011
and PURTD.TD002=TempPURTH.TH012
and PURTD.TD003=TempPURTH.TH013
and PURTD.TD004=TempPURTH.TH004

-- 请购单单身信息档
left join PURTB
on PURTB.TB001=PURTD.TD026
and PURTB.TB002=PURTD.TD027
and PURTB.TB003=PURTD.TD028
and PURTB.TB004=PURTD.TD004

-- 请购单单头信息档
left join PURTA
on PURTA.TA001=PURTB.TB001
and PURTA.TA002=PURTB.TB002

-- 仓库信息档
left join CMSMC
on CMSMC.MC001=TempPURTH.TH009

-- 关联员工基本信息
left join CMSMV
on CMSMV.MV001=PURTA.TA012
"

                                  Using tmpSqlDataReader = tmpSqlCommand.ExecuteReader

                                      While tmpSqlDataReader.Read

                                          Dim tmpDocumentInfo = New DocumentInfo With {
                                              .QGDB = $"{tmpSqlDataReader(0)}".Trim,
                                              .QGDH = Val($"{tmpSqlDataReader(1)}"),
                                              .QGXH = $"{tmpSqlDataReader(2)}".Trim,
                                              .QGRQ = Date.ParseExact($"{tmpSqlDataReader(3)}", "yyyyMMdd", Nothing),
                                              .QGRY = $"{tmpSqlDataReader(4)}".Trim,
                                              .YGXM = $"{tmpSqlDataReader(5)}".Trim,
                                              .QGSL = tmpSqlDataReader(6),
                                              .PH = $"{tmpSqlDataReader(7)}".Trim,
                                              .PM = $"{tmpSqlDataReader(8)}".Trim,
                                              .GG = $"{tmpSqlDataReader(9)}".Trim,
                                              .CK = $"{tmpSqlDataReader(10)}".Trim,
                                              .YSSL = tmpSqlDataReader(11),
                                              .YSRQ = Date.ParseExact($"{tmpSqlDataReader(12)}", "yyyyMMdd", Nothing)
                                          }

                                          AppSettingHelper.Instance.DocumentItems.Add(tmpDocumentInfo)

                                      End While

                                  End Using

                              End Using

                              SqlConn.Close()
                          End Using

                          Console.WriteLine($"表单数 : {AppSettingHelper.Instance.DocumentItems.Count}")
#End Region

                          ' 判断是否有无对应的钉钉账号的ERP用户
                          If Not AppSettingHelper.Instance.DocumentItems.All(Function(s1)
                                                                                 Return AppSettingHelper.Instance.DingTalkUserJobNumberItems.ContainsKey(s1.QGRY)
                                                                             End Function) Then

#Region "获取钉钉AccessToken"
                              uie.Write("获取钉钉AccessToken", 1 * 100 / stepCount)

                              GetDingTalkAccessToken()
#End Region

#Region "获取钉钉部门信息"
                              uie.Write("获取钉钉部门信息", 2 * 100 / stepCount)

                              AppSettingHelper.Instance.DingTalkDepartmentIDItems.Clear()

                              GetDingTalkDepartmentIDItems(1)

                              Console.WriteLine($"部门数 : {AppSettingHelper.Instance.DingTalkDepartmentIDItems.Count}")
#End Region

#Region "获取钉钉员工信息"
                              uie.Write("获取钉钉员工信息", 3 * 100 / stepCount)

                              AppSettingHelper.Instance.DingTalkUserJobNumberItems.Clear()

                              Dim tmpID1 = 1
                              For Each item In AppSettingHelper.Instance.DingTalkDepartmentIDItems

                                  uie.Write($"获取钉钉员工信息 {tmpID1}/{AppSettingHelper.Instance.DingTalkDepartmentIDItems.Count}")
                                  tmpID1 += 1

                                  GetDingTalkUserItems(item)
                              Next

                              Console.WriteLine($"有工号的员工数 : {AppSettingHelper.Instance.DingTalkUserJobNumberItems.Count}")
#End Region

                          End If

#Region "发送工作通知消息"
                          uie.Write("发送工作通知消息", 4 * 100 / stepCount)

                          ' 无对应的钉钉账号的ERP用户
                          Dim NotHaveJobNumberUserItems As New Dictionary(Of String, String)

                          Dim tmpID = 1
                          For Each item In AppSettingHelper.Instance.DocumentItems

                              uie.Write($"发送工作通知消息 {tmpID}/{AppSettingHelper.Instance.DocumentItems.Count}")
                              tmpID += 1

                              ' 判断是否发送过
                              If AppSettingHelper.Instance.SendDocumentIDItems.Contains(String.Join("-",
                                                                                                    {
                                                                                                    item.QGDB,
                                                                                                    item.QGDH,
                                                                                                    item.QGXH,
                                                                                                    $"{item.YSSL:n2}",
                                                                                                    $"{item.YSRQ:d}"
                                                                                                    })) Then
                                  Continue For
                              End If

                              'SendDingTalkWorkMessage("3349644230885065", item)
                              'Exit For

                              ' 判断是否有对应的钉钉账号
                              If Not AppSettingHelper.Instance.DingTalkUserJobNumberItems.ContainsKey(item.QGRY) Then

                                  If Not NotHaveJobNumberUserItems.ContainsKey(item.QGRY) Then
                                      NotHaveJobNumberUserItems.Add(item.QGRY, item.YGXM)

                                  End If

                                  Continue For
                              End If

                              AppSettingHelper.Instance.SendDocumentIDItems.Add(String.Join("-",
                                                                                            {
                                                                                            item.QGDB,
                                                                                            item.QGDH,
                                                                                            item.QGXH,
                                                                                            $"{item.YSSL:n2}",
                                                                                            $"{item.YSRQ:d}"
                                                                                            }))

                              ' 发送消息
                              SendDingTalkWorkMessage(AppSettingHelper.Instance.DingTalkUserJobNumberItems(item.QGRY), item)

                          Next

                          AppSettingHelper.SaveToLocaltion()

                          ' 通知管理员更新账号信息
                          If NotHaveJobNumberUserItems.Count > 0 Then

                              Dim tempAdminMessage = $"无对应的钉钉账号的ERP用户  
> 工号{vbTab}姓名  
{String.Join(vbCrLf, From item In NotHaveJobNumberUserItems
                     Select $"> {item.Key}{vbTab}{item.Value}  ")}"

                              SendDingTalkMessageToAdmin(tempAdminMessage)

                          End If

#End Region

                      End Sub)

        If tmpWindow.Error IsNot Nothing Then
            Wangk.ResourceWPF.Toast.ShowError(Me, tmpWindow.Error.Message)
            Exit Sub
        End If

        If tmpWindow.IsCancel Then
            Wangk.ResourceWPF.Toast.ShowInfo(Me, $"操作已取消")
        Else
            Wangk.ResourceWPF.Toast.ShowSuccess(Me, $"操作完毕")
        End If

    End Sub

#Region "获取钉钉调用企业接口凭证"
    ''' <summary>
    ''' 获取钉钉调用企业接口凭证
    ''' </summary>
    Private Sub GetDingTalkAccessToken()

        Dim client As IDingTalkClient = New DefaultDingTalkClient("https://oapi.dingtalk.com/gettoken")
        Dim req As OapiGettokenRequest = New OapiGettokenRequest()
        req.Appkey = AppSettingHelper.Instance.DingTalkAppKey
        req.Appsecret = AppSettingHelper.Instance.DingTalkAppSecret
        req.SetHttpMethod("GET")
        Dim rsp As OapiGettokenResponse = client.Execute(req, Nothing)
        AppSettingHelper.Instance.DingTalkAccessToken = rsp.AccessToken

    End Sub
#End Region

#Region "获取钉钉部门信息"
    ''' <summary>
    ''' 获取钉钉部门信息
    ''' </summary>
    Private Sub GetDingTalkDepartmentIDItems(parentDepartmentID As Long)

        Dim client As IDingTalkClient = New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/department/listsub")
        Dim req As OapiV2DepartmentListsubRequest = New OapiV2DepartmentListsubRequest()
        req.DeptId = parentDepartmentID
        Dim rsp As OapiV2DepartmentListsubResponse = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)

        If rsp.Result Is Nothing Then
            Exit Sub
        End If

        For Each item In rsp.Result
            AppSettingHelper.Instance.DingTalkDepartmentIDItems.Add(item.DeptId)

            GetDingTalkDepartmentIDItems(item.DeptId)
        Next

    End Sub
#End Region

#Region "获取钉钉部门用户信息"
    ''' <summary>
    ''' 获取钉钉部门用户信息
    ''' </summary>
    Private Sub GetDingTalkUserItems(parentDepartmentID As Long)

        Dim client As IDingTalkClient = New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/list")

        Dim Cursor As Long = 0

        Do

            Dim req As OapiV2UserListRequest = New OapiV2UserListRequest()
            req.DeptId = parentDepartmentID
            req.Cursor = Cursor
            req.Size = 100L
            Dim rsp As OapiV2UserListResponse = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)

            If rsp.Result.List Is Nothing Then
                Exit Sub
            End If

            For Each item In rsp.Result.List

                If String.IsNullOrWhiteSpace(item.JobNumber) Then
                    Continue For
                End If

                If Not AppSettingHelper.Instance.DingTalkUserJobNumberItems.ContainsKey(item.JobNumber) Then
                    AppSettingHelper.Instance.DingTalkUserJobNumberItems.Add(item.JobNumber, item.Userid)
                End If

            Next

            Cursor += 1
        Loop

    End Sub
#End Region

#Region "向钉钉用户发送工作通知消息"
    ''' <summary>
    ''' 向钉钉用户发送工作通知消息
    ''' </summary>
    Private Sub SendDingTalkWorkMessage(dingTalkUserid As String,
                                        doc As DocumentInfo)

        Dim client As IDingTalkClient = New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2")
        Dim req As New OapiMessageCorpconversationAsyncsendV2Request With {
            .AgentId = AppSettingHelper.Instance.DingTalkAgentId,
            .UseridList = dingTalkUserid
        }
        Dim obj1 As New OapiMessageCorpconversationAsyncsendV2Request.MsgDomain With {
            .Msgtype = "markdown"
        }
        Dim obj2 As New OapiMessageCorpconversationAsyncsendV2Request.MarkdownDomain With {
            .Text = $"**<font color=#1296DB>{doc.PM}({doc.PH})</font>**

------
物料规格 : {doc.GG}  
请购日期 : {doc.QGRQ:d}  
请购数量 : {doc.QGSL:n2}  
验收仓库 : {doc.CK}  
验收数量 : {doc.YSSL:n2}",
            .Title = $"{doc.CK} - {doc.PH}"
        }
        obj1.Markdown = obj2
        req.Msg_ = obj1
        Dim rsp As OapiMessageCorpconversationAsyncsendV2Response = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)

    End Sub
#End Region

#Region "发送消息给所有主管理员"
    ''' <summary>
    ''' 发送消息给所有主管理员
    ''' </summary>
    Private Sub SendDingTalkMessageToAdmin(msg As String)

        'SendDingTalkAdminMessage("3349644230885065", msg)

        For Each item In GetDingTalkAdminItems()
            SendDingTalkAdminMessage(item, msg)
        Next

    End Sub
#End Region

#Region "获取主管理员列表"
    ''' <summary>
    ''' 获取主管理员列表
    ''' </summary>
    Private Function GetDingTalkAdminItems() As List(Of String)

        Dim client As New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/user/listadmin")
        Dim req As New OapiUserListadminRequest()
        Dim rsp As OapiUserListadminResponse = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)

        Return (From item In rsp.Result
                Where item.SysLevel = 1
                Select item.Userid).ToList

    End Function
#End Region

#Region "发送消息给主管理员"
    ''' <summary>
    ''' 发送消息给主管理员
    ''' </summary>
    Private Sub SendDingTalkAdminMessage(dingTalkUserid As String,
                                         msg As String)

        Dim client As IDingTalkClient = New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2")
        Dim req As New OapiMessageCorpconversationAsyncsendV2Request With {
            .AgentId = AppSettingHelper.Instance.DingTalkAgentId,
            .UseridList = dingTalkUserid
        }
        Dim obj1 As New OapiMessageCorpconversationAsyncsendV2Request.MsgDomain With {
            .Msgtype = "markdown"
        }
        Dim obj2 As New OapiMessageCorpconversationAsyncsendV2Request.MarkdownDomain With {
            .Text = msg,
            .Title = "管理员消息"
        }
        obj1.Markdown = obj2
        req.Msg_ = obj1
        Dim rsp As OapiMessageCorpconversationAsyncsendV2Response = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)


    End Sub
#End Region

    Private Sub SaveChange(sender As Object, e As RoutedEventArgs)

        Try

            If AppSettingHelper.Instance.StartAutoRun <> StartAutoRun.IsChecked Then

                If StartAutoRun.IsChecked Then

                    Dim shortcutPath As String = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) }\{My.Application.Info.ProductName}.lnk"
                    Dim tmpWshShell = New IWshRuntimeLibrary.WshShell()
                    Dim tmpIWshShortcut As IWshRuntimeLibrary.IWshShortcut = tmpWshShell.CreateShortcut(shortcutPath)
                    With tmpIWshShortcut
                        .TargetPath = System.Reflection.Assembly.GetExecutingAssembly().Location
                        .WorkingDirectory = IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                        .WindowStyle = 1
                        .Description = My.Application.Info.ProductName
                        .IconLocation = .TargetPath
                        .Save()
                    End With

                Else
                    Dim shortcutPath As String = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) }\{My.Application.Info.ProductName}.lnk"
                    Try
                        IO.File.Delete(shortcutPath)
#Disable Warning CA1031 ' Do not catch general exception types
                    Catch ex As Exception
#Enable Warning CA1031 ' Do not catch general exception types
                    End Try

                End If
            End If

            AppSettingHelper.Instance.StartAutoRun = StartAutoRun.IsChecked

            AppSettingHelper.Instance.SearchTimeSpan = New TimeSpan(0, Val(SearchTimeSpan.Value), 0)

            AppSettingHelper.Instance.ERPSqlServerConnStr = ERPSqlServerConnStr.Value

            AppSettingHelper.Instance.DingTalkAgentId = CLng(DingTalkAgentIdStr.Value)
            AppSettingHelper.Instance.DingTalkAppKey = DingTalkAppKey.Value
            AppSettingHelper.Instance.DingTalkAppSecret = DingTalkAppSecret.Value

        Catch ex As Exception
            Wangk.ResourceWPF.Toast.ShowError(Me, ex.Message)
            Exit Sub
        End Try

        SearchTimeSpan.AddHistoryValue()
        ERPSqlServerConnStr.AddHistoryValue()
        DingTalkAgentIdStr.AddHistoryValue()
        DingTalkAppKey.AddHistoryValue()
        DingTalkAppSecret.AddHistoryValue()

        AppSettingHelper.SaveToLocaltion()

        Wangk.ResourceWPF.Toast.ShowSuccess(Me, "修改成功")

    End Sub

    Private Sub NotSaveChange(sender As Object, e As RoutedEventArgs)

    End Sub

End Class
