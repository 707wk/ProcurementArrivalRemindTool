Public Class DocumentInfo

    ''' <summary>
    ''' 请购单别
    ''' </summary>
    Public QGDB As String
    ''' <summary>
    ''' 请购单号
    ''' </summary>
    Public QGDH As String
    ''' <summary>
    ''' 请购序号
    ''' </summary>
    Public QGXH As String

    ''' <summary>
    ''' 请购日期
    ''' </summary>
    Public QGRQ As Date

    ''' <summary>
    ''' 请购人员
    ''' </summary>
    Public QGRY As String
    ''' <summary>
    ''' 员工姓名
    ''' </summary>
    Public YGXM As String

    ''' <summary>
    ''' 请购数量
    ''' </summary>
    Public QGSL As Decimal

    ''' <summary>
    ''' 品号
    ''' </summary>
    Public PH As String
    ''' <summary>
    ''' 品名
    ''' </summary>
    Public PM As String
    ''' <summary>
    ''' 规格
    ''' </summary>
    Public GG As String

    ''' <summary>
    ''' 仓库
    ''' </summary>
    Public CK As String

    ''' <summary>
    ''' 验收数量
    ''' </summary>
    Public YSSL As Decimal

    ''' <summary>
    ''' 验收日期
    ''' </summary>
    Public YSRQ As Date

    ''' <summary>
    ''' 文档主键
    ''' </summary>
    Public ReadOnly Property KeyStr As String
        Get
            Return Wangk.Hash.SHAHelper.GetStrSHA512(String.Join("-",
                                                                 {
                                                                 QGDB,
                                                                 QGDH,
                                                                 QGXH,
                                                                 $"{YSSL:n2}",
                                                                 $"{YSRQ:d}"
                                                                 }))
        End Get
    End Property

End Class
