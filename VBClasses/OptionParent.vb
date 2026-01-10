Imports System.Windows.Forms
Namespace VBClasses
    Public Class OptionParent
        Public check As New OptionsCheckbox
        Public combo As New OptionsCombo
        Public radio As New OptionsRadioButtons
        Public sliders As New OptionsSliders
        Public traceName As String
        Public strOut As String
        Public Shared Function FindSlider(opt As String) As TrackBar
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Sliders") Then
                    For j = 0 To frm.myTrackbars.Count - 1
                        If frm.myLabels(j).text.startswith(opt) Then Return frm.myTrackbars(j)
                    Next
                End If
            Next
            Debug.WriteLine("OptionParent.FindSlider failed.  " + opt + " was not found.")
            Return Nothing
        End Function
        Public Shared Function FindCheckBox(opt As String) As CheckBox
            For Each frm In Application.OpenForms
                If frm.text.endswith(" CheckBoxes") Then
                    For j = 0 To frm.Box.Count - 1
                        If frm.Box(j).text = opt Then Return frm.Box(j)
                    Next
                End If
            Next
            Debug.WriteLine("OptionParent.findCheckBox failed.  " + opt + " was not found.")
            Return Nothing
        End Function
        Public Shared Function findRadio(opt As String) As RadioButton
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Radio Buttons") Then
                    For j = 0 To frm.check.count - 1
                        If frm.check(j).text = opt Then Return frm.check(j)
                    Next
                End If
            Next
            Debug.WriteLine("OptionParent.findRadio failed.  " + opt + " was not found.")
            Return Nothing
        End Function
        Public Shared Function findRadioText(ByRef radioList As List(Of RadioButton)) As String
            For Each rad In radioList
                If rad.Checked Then Return rad.Text
            Next
            Return radioList(0).Text
        End Function
        Public Shared Function findRadioIndex(ByRef radioList As List(Of RadioButton)) As Integer
            For i = 0 To radioList.Count - 1
                If radioList(i).Checked Then Return i
            Next
            Return 0
        End Function
        Public Shared Function FindFrm(title As String) As System.Windows.Forms.Form
            For Each frm In Application.OpenForms
                If frm.text = title Then Return frm
            Next
            Return Nothing
        End Function
        Public Sub New()
            traceName = Me.GetType.Name
        End Sub
    End Class
End Namespace