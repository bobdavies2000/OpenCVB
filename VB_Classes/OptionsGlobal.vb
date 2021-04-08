Imports System.Windows.Forms
Public Class OptionsGlobal
    Private Sub MaxRange_Scroll(sender As Object, e As EventArgs) Handles MaxRange.Scroll
        maxCount.Text = CStr(MaxRange.Value)
    End Sub
    Private Sub MinRange_Scroll(sender As Object, e As EventArgs) Handles MinRange.Scroll
        minCount.Text = CStr(MinRange.Value)
    End Sub
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = aOptions
        MinRange.Value = GetSetting("OpenCVB", "MinRangeDepth", "MinRangeDepth", 200)
        MaxRange.Value = GetSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", 4000)
        maxCount.Text = CStr(MaxRange.Value)
        minCount.Text = CStr(MinRange.Value)
    End Sub
    Private Sub OptionsGlobal_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SaveSetting("OpenCVB", "MinRangeDepth", "MinRangeDepth", MinRange.Value)
        SaveSetting("OpenCVB", "MaxRangeDepth", "MaxRangeDepth", MaxRange.Value)
    End Sub
End Class