''' <summary>
''' 钉钉节点信息
''' </summary>
Public Class DingTalkNodeInfo

    ''' <summary>
    ''' 部门ID,部门节点用
    ''' </summary>
    Public Property DepartmentID As Long

    ''' <summary>
    ''' 子节点,部门节点用
    ''' </summary>
    Public Property Nodes As New List(Of DingTalkNodeInfo)

    ''' <summary>
    ''' 用户ID,用户节点用
    ''' </summary>
    Public Property UserID As String

    ''' <summary>
    ''' 职位名称,用户节点用
    ''' </summary>
    Public Property Title As String

    ''' <summary>
    ''' 工号,用户节点用
    ''' </summary>
    Public Property JobNumber As String

    ''' <summary>
    ''' 名称
    ''' </summary>
    Public Property Name As String

    Public ReadOnly Property NameStr As String
        Get

            If IsUser Then
                Return Name

            Else
                Return $"{Name} ({Nodes.Count})"

            End If

        End Get
    End Property

    ''' <summary>
    ''' 是否是用户节点
    ''' </summary>
    Public Property IsUser As Boolean

    ''' <summary>
    ''' 节点图标
    ''' </summary>
    Public ReadOnly Property ICOPath As String
        Get

            If Not IsUser Then
                Return "../../../Resources/DingTalkNodeType01_16px.png"

            Else
                Return "../../../Resources/DingTalkNodeType02_16px.png"

            End If

        End Get
    End Property

    ''' <summary>
    ''' 是否选中
    ''' </summary>
    ''' <returns></returns>
    Public Property Selected As Boolean

    ''' <summary>
    ''' 启用选择
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property EnableSelected As Boolean
        Get

            If Not IsUser Then Return False

            If String.IsNullOrWhiteSpace(JobNumber) Then Return False

            Return True

        End Get
    End Property

End Class
