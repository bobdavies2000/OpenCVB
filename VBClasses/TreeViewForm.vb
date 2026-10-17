Public Class TreeviewForm
    Dim treeData As New List(Of String) ' treedata is used to trigger a rebuild of the tree nodes.
    Dim nodeList As New List(Of String) ' this is used to define the tag but is not referenced later.
    Dim titleStr = " - Click on any node to review the algorithm's output."
    Public Sub TreeviewForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        TreeView1.Height = Me.Height
        PercentTime.Width = Me.Width - 100
        PercentTime.Height = TreeView1.Height
    End Sub
    Private Sub TreeviewForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        task.Settings.TreeViewLeft = Me.Left
        task.Settings.TreeViewTop = Me.Top
        task.Settings.TreeViewWidth = Me.Width
        task.Settings.TreeViewHeight = Me.Height
    End Sub
    Public Sub BuildTreeView(tree As TreeView, paths As IEnumerable(Of String))
        tree.BeginUpdate()
        tree.Nodes.Clear()
        For Each path In paths
            Dim parts = path.Split("\"c)
            Dim currentNodes = tree.Nodes

            For i = 0 To parts.Length - 1
                Dim part = parts(i)
                Dim isLeaf = (i = parts.Length - 1)

                ' Try to find an existing node to reuse (only when not at leaf)
                Dim found As TreeNode = Nothing
                If Not isLeaf Then
                    For Each n As TreeNode In currentNodes
                        If n.Text = part Then
                            found = n
                            Exit For
                        End If
                    Next
                End If

                ' Create if missing, or always create at leaf to allow duplicate names
                If found Is Nothing Then
                    found = currentNodes.Add(part)
                    found.ToolTipText = path + "\"
                    nodeList.Add(found.Text)
                End If

                If found IsNot Nothing Then currentNodes = found.Nodes
            Next
        Next

        tree.EndUpdate()
        tree.ExpandAll()
    End Sub

    Public Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If task Is Nothing Then Exit Sub
        If task.cpu.callTrace.Count <> treeData.Count Then
            treeData.Clear()
            For Each td In task.cpu.callTrace
                If td.EndsWith("\"c) Then td = td.Substring(0, td.Length - 1)
                treeData.Add(td)
            Next
            BuildTreeView(TreeView1, treeData)
        End If

        PercentTime.Text = CPUTime.PrepareReport(treeData)
        PercentTime.Refresh()
    End Sub
    Private Sub TreeviewForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TreeView1.Dock = DockStyle.Fill
        TreeView1.SendToBack()

        Me.Location = New System.Drawing.Point(task.Settings.TreeViewLeft, task.Settings.TreeViewTop)
        Me.Size = New System.Drawing.Size(task.Settings.TreeViewWidth, task.Settings.TreeViewHeight)

        PercentTime.Left = 250
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        task.cpu.displayObjectName = e.Node.Text
        task.cpu.displayObjectIndex = task.cpu.callTrace.IndexOf(e.Node.ToolTipText)
        Timer2_Tick(sender, e)
    End Sub
End Class
