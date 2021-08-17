Imports DingTalk.Api
Imports DingTalk.Api.Request
Imports DingTalk.Api.Response

Public Class SelectUsersWindow

    ''' <summary>
    ''' 启用的用户列表
    ''' </summary>
    Public EnableUserList As New List(Of DingTalkUserInfo)

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        UIHelper.InitChildWindowStyle(Me)

        SetUserEnable(AppSettingHelper.Instance.DingTalkDepartmentRootNode)

        DingTalkDepartmentTree.ItemsSource = {AppSettingHelper.Instance.DingTalkDepartmentRootNode}

    End Sub

    ''' <summary>
    ''' 设置用户选择状态
    ''' </summary>
    Private Sub SetUserEnable(node As DingTalkNodeInfo)

        If node.EnableSelected Then

            node.Selected = EnableUserList.Exists(Function(a)
                                                      Return a.JobNumber = node.JobNumber
                                                  End Function)

        End If

        For Each item In node.Nodes
            SetUserEnable(item)
        Next

    End Sub

    ''' <summary>
    ''' 获取用户选择状态
    ''' </summary>
    Private Sub GetUserEnable(node As DingTalkNodeInfo)

        If node.EnableSelected AndAlso
            node.Selected Then

            If EnableUserList.Exists(Function(a)
                                         Return a.UserID = node.UserID
                                     End Function) Then
            Else
                EnableUserList.Add(New DingTalkUserInfo With {
                                   .UserID = node.UserID,
                                   .Name = node.Name,
                                   .JobNumber = node.JobNumber
                                   })
            End If


        End If

        For Each item In node.Nodes
            GetUserEnable(item)
        Next

    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)

        EnableUserList = New List(Of DingTalkUserInfo)

        GetUserEnable(AppSettingHelper.Instance.DingTalkDepartmentRootNode)

    End Sub

#Region "获取部门及部门员工信息"
    Private Sub GetDingTalkDepartmentInfo(sender As Object, e As RoutedEventArgs)

        Dim tmpWindow As New Wangk.ResourceWPF.BackgroundWork(Me) With {
            .Title = "初始化"
        }

        tmpWindow.Run(Sub(uie)
                          Dim stepCount = 4

#Region "获取钉钉AccessToken"
                          uie.Write("获取钉钉AccessToken", 0 * 100 / stepCount)

                          GetDingTalkAccessToken()
#End Region

#Region "获取钉钉公司信息"
                          uie.Write("获取钉钉公司信息", 1 * 100 / stepCount)

                          AppSettingHelper.Instance.DingTalkDepartmentRootNode = GetDingTalkCompanyInfo()
#End Region

#Region "获取钉钉部门信息"
                          uie.Write("获取钉钉部门信息", 2 * 100 / stepCount)

                          DepartmentCount = 1

                          GetDingTalkDepartmentInfoItems(AppSettingHelper.Instance.DingTalkDepartmentRootNode, uie)
#End Region

#Region "获取钉钉员工信息"
                          uie.Write("获取钉钉员工信息", 3 * 100 / stepCount)

                          ReadDepartmentCount = 0

                          GetDingTalkUserInfoItems(AppSettingHelper.Instance.DingTalkDepartmentRootNode, uie)
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

        SetUserEnable(AppSettingHelper.Instance.DingTalkDepartmentRootNode)

        DingTalkDepartmentTree.ItemsSource = {AppSettingHelper.Instance.DingTalkDepartmentRootNode}

    End Sub
#End Region

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

#Region "获取钉钉公司信息"
    ''' <summary>
    ''' 获取钉钉公司信息
    ''' </summary>
    Private Function GetDingTalkCompanyInfo() As DingTalkNodeInfo

        Dim client As New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/department/get")
        Dim req As New OapiV2DepartmentGetRequest()
        req.DeptId = 1L
        Dim rsp = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)

        Return New DingTalkNodeInfo With {
            .Name = rsp.Result.Name,
            .DepartmentID = 1
        }

    End Function
#End Region

    Private DepartmentCount As Integer = 0

    Private ReadDepartmentCount As Integer = 0

#Region "获取钉钉部门信息"
    ''' <summary>
    ''' 获取钉钉部门信息
    ''' </summary>
    Private Sub GetDingTalkDepartmentInfoItems(parentDepartment As DingTalkNodeInfo,
                                               uie As Wangk.ResourceWPF.IBackgroundWorkEventArgs)

        Dim client As IDingTalkClient = New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/department/listsub")
        Dim req As OapiV2DepartmentListsubRequest = New OapiV2DepartmentListsubRequest()
        req.DeptId = parentDepartment.DepartmentID
        Dim rsp As OapiV2DepartmentListsubResponse = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)

        If rsp.Result Is Nothing Then
            Exit Sub
        End If

        For Each item In rsp.Result
            Dim tmpDingTalkNodeInfo As New DingTalkNodeInfo With {
                .DepartmentID = item.DeptId,
                .Name = item.Name
            }

            DepartmentCount += 1
            uie.Write($"获取钉钉部门信息 {DepartmentCount}")

            parentDepartment.Nodes.Add(tmpDingTalkNodeInfo)

            GetDingTalkDepartmentInfoItems(tmpDingTalkNodeInfo, uie)
        Next

    End Sub
#End Region

#Region "获取钉钉部门用户信息"
    ''' <summary>
    ''' 获取钉钉部门用户信息
    ''' </summary>
    Private Sub GetDingTalkUserInfoItems(parentDepartment As DingTalkNodeInfo,
                                         uie As Wangk.ResourceWPF.IBackgroundWorkEventArgs)

        Dim client As IDingTalkClient = New DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/list")

        Dim Cursor As Long = 0

        ReadDepartmentCount += 1
        uie.Write($"获取钉钉员工信息 {ReadDepartmentCount}/{DepartmentCount}")

        Do

            Dim req As OapiV2UserListRequest = New OapiV2UserListRequest()
            req.DeptId = parentDepartment.DepartmentID
            req.Cursor = Cursor
            req.Size = 100L
            Dim rsp As OapiV2UserListResponse = client.Execute(req, AppSettingHelper.Instance.DingTalkAccessToken)

            If rsp.Result.List Is Nothing Then
                Exit Do
            End If

            For Each item In rsp.Result.List

                Dim tmpDingTalkNodeInfo As New DingTalkNodeInfo With {
                    .UserID = item.Userid,
                    .Name = item.Name,
                    .JobNumber = item.JobNumber,
                    .Title = item.Title,
                    .IsUser = True
                }

                parentDepartment.Nodes.Add(tmpDingTalkNodeInfo)

            Next

            If Not rsp.Result.HasMore Then
                Exit Do
            End If

            Cursor += req.Size
        Loop

        ' 获取子部门用户列表
        For Each item In parentDepartment.Nodes

            If item.IsUser Then
                Continue For
            End If

            GetDingTalkUserInfoItems(item, uie)

        Next

    End Sub
#End Region

End Class
