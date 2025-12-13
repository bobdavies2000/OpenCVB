Imports System.IO
Imports cv = OpenCvSharp
Namespace MainForm
    Public Class AtoZ
        Public homeDir As DirectoryInfo
        Public selectedGroup As String = ""

        Private Sub Groups_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Me.Width = 1220
            Me.Height = 785
            GroupDataView.Columns.Clear()
            GroupDataView.Rows.Clear()
            Dim newFont As New Font("Arial", 12, FontStyle.Bold)
            GroupDataView.DefaultCellStyle.Font = newFont

            Me.Text = "Click on an algorithm group to jump to the first algorithm in that group."
            GroupDataView.CellBorderStyle = DataGridViewCellBorderStyle.None
            GroupDataView.ColumnHeadersVisible = False
            GroupDataView.RowHeadersVisible = False
            Dim grplines = File.ReadAllLines(homeDir.FullName + "/GroupButtonList.txt")

            Dim colsPerRow = 8
            Dim rowsPerCol = 32
            For i = 0 To colsPerRow - 1
                Dim column As New DataGridViewTextBoxColumn()
                column.Name = "Column" & i
                column.HeaderText = "     "
                GroupDataView.Columns.Add(column)
            Next

            Dim rowCount = 0
            For i = 0 To grplines.Count - 1
                Dim row As String() = New String(colsPerRow - 1) {}
                Dim index As Integer = 0
                For j = i To grplines.Count - 1 Step rowsPerCol
                    If index >= row.Length Then Exit For
                    row(index) = grplines(j)
                    index += 1
                Next
                GroupDataView.Rows.Add(row)
                rowCount += 1
                If rowCount >= rowsPerCol Then Exit For
            Next
        End Sub

        Private Sub GroupDataView_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles GroupDataView.CellContentClick
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                selectedGroup = GroupDataView.Rows(e.RowIndex).Cells(e.ColumnIndex).Value?.ToString()
                Me.DialogResult = DialogResult.OK
                Me.Close()
            End If
        End Sub

        Private Sub Groups_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
            If e.KeyCode = Keys.Escape Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            End If
        End Sub
    End Class
End Namespace
