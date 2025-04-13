Imports System.Windows.Forms
Public Class OptionParent : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public traceName As String
    Public strOut As String
    Public Function FindSlider(opt As String) As TrackBar
        Try
            Application.DoEvents()
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Sliders") Then
                    For j = 0 To frm.myTrackbars.Count - 1
                        If frm.myLabels(j).text.startswith(opt) Then Return frm.myTrackbars(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine("FindSlider failed." + vbCrLf +
                            "Did the list of forms changed while iterating.  Not critical." + ex.Message)
        End Try
        Debug.WriteLine("A slider was Not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

        Return Nothing
    End Function
    Public Function FindCheckBox(opt As String) As CheckBox
        Try
            Application.DoEvents()
            For Each frm In Application.OpenForms
                If frm.text.endswith(" CheckBoxes") Then
                    For j = 0 To frm.Box.Count - 1
                        If frm.Box(j).text = opt Then Return frm.Box(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine("optiBase.FindCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
        End Try
        Return Nothing
    End Function
    Public Function findRadio(opt As String) As RadioButton
        Dim index As Integer
        Dim radio = searchForms(opt, index)
        If radio Is Nothing Then Return Nothing
        Return radio(index)
    End Function
    Public Function findRadioText(ByRef radioList As List(Of RadioButton)) As String
        Application.DoEvents()
        For Each rad In radioList
            If rad.Checked Then Return rad.Text
        Next
        Return radioList(0).Text
    End Function
    Public Function findRadioIndex(ByRef radioList As List(Of RadioButton)) As String
        Application.DoEvents()
        For i = 0 To radioList.Count - 1
            If radioList(i).Checked Then Return i
        Next
        Return 0
    End Function
    Private Function searchForms(opt As String, ByRef index As Integer)
        Try
            Application.DoEvents()
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Radio Buttons") Then
                    For j = 0 To frm.check.count - 1
                        If frm.check(j).text = opt Then
                            index = j
                            Return frm.check
                        End If
                    Next
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine("optibase.findRadioForm failed.  The application list of forms changed while iterating.  Not critical.")
        End Try
        Return Nothing
    End Function
    Public Function FindFrm(title As String) As System.Windows.Forms.Form
        On Error Resume Next
        Application.DoEvents()
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    Public Sub New()
        traceName = Me.GetType.Name
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If allOptions IsNot Nothing Then allOptions.Close()
        sliders.Dispose()
        check.Dispose()
        radio.Dispose()
        combo.Dispose()
    End Sub
    Public Enum FeatureSrc
        GoodFeaturesFull = 0
        GoodFeaturesGrid = 1
        Agast = 2
        BRISK = 3
        Harris = 4
        FAST = 5
        LineInput = 6
        gridPoints = 7
    End Enum
End Class