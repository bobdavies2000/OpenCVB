Imports System.ComponentModel
Imports VBClasses
Public Class TreeviewForm
    Dim botDistance As Integer
    Dim treeData As New List(Of String) ' treedata is used to trigger a rebuild of the tree nodes.
    Dim nodeList As New List(Of String) ' this is used to define the tag but is not referenced later.
    Dim taskIndices As New List(Of Integer)
    Dim titleStr = " - Click on any node to review the algorithm's output."
    Public Sub TreeviewForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        TreeView1.Height = Me.Height
        PercentTime.Height = TreeView1.Height
    End Sub
    Private Sub TreeviewForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        atask.Settings.TreeViewLeft = Me.Left
        atask.Settings.TreeViewTop = Me.Top
        atask.Settings.TreeViewWidth = Me.Width
        atask.Settings.TreeViewHeight = Me.Height
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
    Public Sub BuildTreeView(tree As TreeView, paths As IEnumerable(Of String))
        tree.BeginUpdate()
        tree.Nodes.Clear()
        For Each path In paths
            Dim parts = path.Split("\"c)
            Dim currentNodes = tree.Nodes
            Dim currentNode As TreeNode = Nothing

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
                    found.Tag = nodeList.Count
                    nodeList.Add(found.Text)
                End If

                If found IsNot Nothing Then
                    currentNode = found
                    currentNodes = found.Nodes
                End If
            Next
        Next

        tree.Nodes(tree.Nodes.Count - 1).Remove()

        tree.EndUpdate()
        tree.ExpandAll()
    End Sub

    Public Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If atask Is Nothing Then Exit Sub
        If atask.cpu.callTrace.Count <> treeData.Count Then
            treeData.Clear()
            For Each td In atask.cpu.callTrace
                If td.EndsWith("\") Then td = td.Substring(0, td.Length - 1)
                treeData.Add(td)
            Next
            BuildTreeView(TreeView1, treeData)
        End If

        PercentTime.Text = atask.cpu.PrepareReport(treeData)
        PercentTime.Refresh()
    End Sub
    Private Sub TreeviewForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TreeView1.Dock = DockStyle.Fill
        TreeView1.SendToBack()

        Me.Location = New System.Drawing.Point(atask.Settings.TreeViewLeft, atask.Settings.TreeViewTop)
        Me.Size = New System.Drawing.Size(atask.Settings.TreeViewWidth, atask.Settings.TreeViewHeight)

        PercentTime.Width = 250
        PercentTime.Left = 250
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        atask.cpu.displayObjectName = e.Node.Text
        atask.cpu.indexTask = e.Node.Tag
        Timer2_Tick(sender, e)
    End Sub
End Class