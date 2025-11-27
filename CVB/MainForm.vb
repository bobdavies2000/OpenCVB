Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO

Namespace CVB
    Public Class MainForm : Inherits Form
        Dim isPlaying As Boolean = False
        Dim projectFilePath As String = ""
        Const MAX_RECENT = 50
        Dim algHistory As New List(Of String)
        Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
        Private campic(3) As PictureBox
        Public Sub jumpToAlgorithm(algName As String)
            If AvailableAlgorithms.Items.Contains(algName) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = algName
            End If
            'settings.algorithm = AvailableAlgorithms.Text
            'jsonfs.write()
        End Sub
        Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
            Dim item = TryCast(sender, ToolStripMenuItem)
            If AvailableAlgorithms.Items.Contains(item.Text) = False Then
                MessageBox.Show("That algorithm was not found" + vbCrLf + vbCrLf + "The name may have changed or " + vbCrLf +
                       "The currently selected group does not contain " + item.Text + vbCrLf + "Change the group to <All> to guarantee access.")
            Else
                jumpToAlgorithm(item.Text)
            End If
        End Sub
        Public Sub setupAlgorithmHistory()
            For i = 0 To MAX_RECENT - 1
                Dim nextA = GetSetting("OpenCVB", "algHistory" + CStr(i), "algHistory" + CStr(i), "recent algorithm " + CStr(i))
                If nextA = "" Then Exit For
                If algHistory.Contains(nextA) = False Then
                    algHistory.Add(nextA)
                    RecentList.DropDownItems.Add(nextA)
                    AddHandler RecentList.DropDownItems(RecentList.DropDownItems.Count - 1).Click,
                               AddressOf algHistory_Clicked
                End If
            Next
        End Sub

        Public Sub New(Optional projectFile As String = "")
            InitializeComponent()
            projectFilePath = projectFile
        End Sub

        Private Sub InitializeCameraPictureBoxes()
            If campic(0) IsNot Nothing Then
                Exit Sub
            End If

            Dim padding As Integer = 12
            Dim topOffset As Integer = MainToolStrip.Bottom + padding
            Dim availableWidth As Integer = Math.Max(0, ClientSize.Width - ((campic.Length + 1) * padding))
            Dim boxWidth As Integer = Math.Max(150, availableWidth \ campic.Length)
            Dim boxHeight As Integer = 160

            For i = 0 To campic.Length - 1
                Dim pb As New PictureBox() With {
                    .Name = $"campic{i}",
                    .BorderStyle = BorderStyle.FixedSingle,
                    .BackColor = Color.Black,
                    .SizeMode = PictureBoxSizeMode.Zoom,
                    .Size = New Size(boxWidth, boxHeight),
                    .Location = New Point(padding + i * (boxWidth + padding), topOffset),
                    .Anchor = AnchorStyles.Top Or AnchorStyles.Left,
                    .TabStop = False
                }

                campic(i) = pb
                Controls.Add(pb)
            Next
        End Sub

        Private Sub NewToolStripButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            ' Toggle between Play and Pause
            isPlaying = Not isPlaying
            PausePlayButton.Image = If(isPlaying, New Bitmap(CurDir() + "/Data/PauseButton.png"),
                                                  New Bitmap(CurDir() + "/Data/Run.png"))
        End Sub

        Private Sub SettingsToolStripButton_Click(sender As Object, e As EventArgs) Handles SettingsToolStripButton.Click
            MessageBox.Show("Settings button clicked!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information)
            AlgDescription.Visible = True
        End Sub

        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            ' Set the current directory to the project path (where .vbproj file is located)
            Dim projectDir As DirectoryInfo = Nothing
            If Not String.IsNullOrEmpty(projectFilePath) AndAlso File.Exists(projectFilePath) Then
                projectDir = New DirectoryInfo(Path.GetDirectoryName(projectFilePath))
                Directory.SetCurrentDirectory(projectDir.FullName)
            End If

            PausePlayButton.PerformClick()
            PausePlayButton.Image = New Bitmap(CurDir() + "/Data/PauseButton.png")

            setupAlgorithmHistory()
            InitializeCameraPictureBoxes()

            AlgDescription.Size = New Size(550, AlgDescription.Size.Height)
            AlgDescription.Text = "Description of the algorithm"
            AlgDescription.Visible = True
        End Sub


        Private Sub Magnifier_Click(sender As Object, e As EventArgs) Handles Magnifier.Click

        End Sub
    End Class
End Namespace

