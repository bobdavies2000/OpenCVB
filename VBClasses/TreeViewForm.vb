Imports VBClasses
Public Class TreeviewForm
    Dim botDistance As Integer
    Dim treeData As New List(Of String)
    Dim moduleList As New List(Of String) ' the list of all active algorithms.
    Dim PercentTimes As New SortedList(Of Single, String)(New compareAllowIdenticalSingle)
    Dim titleStr = " - Click on any node to review the algorithm's output."
    Public Sub TreeviewForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        TreeView1.Height = Me.Height
        PercentTime.Height = TreeView1.Height
    End Sub
    Private Function FindRecursive(ByVal tNode As TreeNode, name As String) As TreeNode
        Dim tn As TreeNode
        For Each tn In tNode.Nodes
            If tn.Tag = name Then Return tn
            Dim rnode = FindRecursive(tn, name)
            If rnode IsNot Nothing Then Return rnode
        Next
        Return Nothing
    End Function
    Private Function getNode(tv As TreeView, name As String) As TreeNode
        For Each n In tv.Nodes
            If n.tag = name Then Return n
            Dim rnode = FindRecursive(n, name)
            If rnode IsNot Nothing Then Return rnode
        Next
        Return Nothing
    End Function
    Public Sub updateTree(callTrace As List(Of String))
        If callTrace.Count = 0 Then Exit Sub
        moduleList.Clear()

        Dim tv = TreeView1
        tv.Nodes.Clear()
        Dim rootcall = Trim(callTrace(0))

        Dim title = Mid(rootcall, 1, Len(rootcall) - 1)
        Me.Text = title + titleStr

        Dim n = tv.Nodes.Add(title)
        n.Tag = rootcall

        Dim entryCount = 1
        For nodeLevel = 0 To 100 ' this loop will terminate after the depth of the nesting.  100 is excessive insurance deep nesting may occur.
            Dim alldone = True

            For i = 1 To callTrace.Count - 1
                Dim fullname = callTrace(i)
                Dim split() = fullname.Split("\")
                If split.Count = nodeLevel + 3 Then
                    alldone = False
                    Dim node = getNode(tv, fullname)
                    If node Is Nothing Then
                        If nodeLevel = 0 Then
                            node = tv.Nodes(nodeLevel).Nodes.Add(split(nodeLevel + 1))
                        Else
                            Dim parent = Mid(fullname, 1, Len(fullname) - Len(split(nodeLevel + 1)) - 1)
                            If parent <> rootcall Then
                                node = getNode(tv, parent)
                                If node Is Nothing Then Continue For
                                node = node.Nodes.Add(split(nodeLevel + 1))
                            End If
                        End If
                    Else
                        node = node.Nodes.Add(split(nodeLevel))
                    End If
                    entryCount += 1
                    node.Tag = fullname
                End If
            Next
            If alldone Then Exit For ' we didn't find any more nodes to add.
        Next

        For Each sn In callTrace
            If sn = "" Then Exit For
            Dim split() = sn.Split("\")
            If split.Length > 1 Then treeData.Add(split(split.Length - 2))
        Next

        tv.ExpandAll()
        tv.HideSelection = False
        tv.SelectedNode = n
    End Sub
    Private Class compareAllowIdenticalSingle : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Static saveCount As Integer
        If taskAlg.cpu.callTrace.Count <> saveCount Then
            saveCount = taskAlg.cpu.callTrace.Count
            updateTree(New List(Of String)(taskAlg.cpu.callTrace))
        End If

        PercentTime.Text = taskAlg.cpu.PrepareReport(treeData)
    End Sub
    Private Sub CheckIfOffScreen()
        Dim formRect As Rectangle = Me.Bounds
        Dim screenBounds As Rectangle = Screen.PrimaryScreen.WorkingArea ' Use WorkingArea to exclude taskbar

        ' Check if any part of the form is visible on the screen
        If Not screenBounds.IntersectsWith(formRect) Then
            ' The entire form is off the screen
            MessageBox.Show("Form is completely off-screen!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)

            ' Optionally, you might want to move the form back onto the screen
            ' For example, move it to the center of the primary screen
            Me.StartPosition = FormStartPosition.Manual
            Me.Location = New Point(screenBounds.Left + (screenBounds.Width - Me.Width) \ 2,
                                    screenBounds.Top + (screenBounds.Height - Me.Height) \ 2)
        End If
    End Sub
    Private Sub TreeviewForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TreeView1.Dock = DockStyle.Fill
        TreeView1.SendToBack()

        Me.Location = New Point(taskAlg.Settings.TreeViewLeft, taskAlg.Settings.TreeViewTop)
        Me.Size = New Size(taskAlg.Settings.TreeViewWidth, taskAlg.Settings.TreeViewHeight)

        PercentTime.Width = 250
        PercentTime.Left = 250

        CheckIfOffScreen()
    End Sub
    Private Sub TreeviewForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        taskAlg.Settings.TreeViewLeft = Me.Left
        taskAlg.Settings.TreeViewTop = Me.Top
        taskAlg.Settings.TreeViewWidth = Me.Width
        taskAlg.Settings.TreeViewHeight = Me.Height
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        Dim algorithm = e.Node.Text
        Dim split = e.Node.Text.Split(" ")
        taskAlg.cpu.displayObjectName = split(0)
    End Sub
End Class