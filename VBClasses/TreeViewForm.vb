Imports System.ComponentModel
Imports VBClasses
Public Class TreeviewForm
    Dim botDistance As Integer
    Dim treeData As New List(Of String) ' treedata is used to trigger a rebuild of the tree nodes.
    Dim taskIndices As New List(Of Integer)
    Dim titleStr = " - Click on any node to review the algorithm's output."
    Public Sub TreeviewForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        TreeView1.Height = Me.Height
        PercentTime.Height = TreeView1.Height
    End Sub
    Private Sub TreeviewForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        task.Settings.TreeViewLeft = Me.Left
        task.Settings.TreeViewTop = Me.Top
        task.Settings.TreeViewWidth = Me.Width
        task.Settings.TreeViewHeight = Me.Height
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
                        If nodeLevel < split.Count Then
                            If split(nodeLevel) <> "" Then node = node.Nodes.Add(split(nodeLevel))
                        End If
                    End If
                    entryCount += 1
                    node.Tag = fullname
                End If
            Next
            If alldone Then Exit For ' we didn't find any more nodes to add.
        Next

        For i = 0 To callTrace.Count - 1
            treeData.Add(callTrace(i))
        Next

        tv.ExpandAll()
        tv.HideSelection = False
        tv.SelectedNode = n
    End Sub
    Public Sub BuildTreeView(tree As TreeView, paths As IEnumerable(Of String))
        tree.BeginUpdate()
        tree.Nodes.Clear()

        For Each path In paths
            Dim parts = path.Split("\"c)
            Dim currentNodes = tree.Nodes
            Dim currentNode As TreeNode = Nothing

            For Each part In parts
                ' Try to find an existing node
                Dim found As TreeNode = Nothing
                For Each n As TreeNode In currentNodes
                    If n.Text = part Then
                        found = n
                        Exit For
                    End If
                Next

                ' Create if missing
                If found Is Nothing Then
                    found = currentNodes.Add(part)
                End If

                currentNode = found
                currentNodes = found.Nodes
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
                If td.EndsWith("\") Then td = td.Substring(0, td.Length - 1)
                treeData.Add(td)
            Next
            BuildTreeView(TreeView1, treeData)

            Dim tempList As New List(Of String)(treeData)
            treeData.Clear()
            For Each td In tempList
                Dim split = td.Split("\")
                treeData.Add(split.Last)
            Next
        End If

        PercentTime.Text = task.cpu.PrepareReport(treeData)
        PercentTime.Refresh()
    End Sub
    Private Sub TreeviewForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TreeView1.Dock = DockStyle.Fill
        TreeView1.SendToBack()

        Me.Location = New System.Drawing.Point(task.Settings.TreeViewLeft, task.Settings.TreeViewTop)
        Me.Size = New System.Drawing.Size(task.Settings.TreeViewWidth, task.Settings.TreeViewHeight)

        PercentTime.Width = 250
        PercentTime.Left = 250
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        For i = 0 To treeData.Count - 1
            If treeData(i) = e.Node.Text Then
                task.cpu.indexTask = i
                Exit For
            End If
        Next
        Timer2_Tick(sender, e)
    End Sub
End Class