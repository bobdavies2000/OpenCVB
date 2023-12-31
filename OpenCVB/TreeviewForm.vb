﻿Imports System.ComponentModel
Imports System.Windows.Forms
Public Class TreeviewForm
    Dim botDistance As Integer
    Dim addendum As String = ""
    Dim moduleList As New List(Of String) ' the list of all active algorithms.
    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        Me.TreeViewTimer.Enabled = False
        Dim algorithm = e.Node.Text
        Dim split = Me.Text.Split(" ")
        If PercentTime.Text.Contains(algorithm) = False And algorithm <> split(0) Then
            addendum = algorithm + " is not active at this time.  " + split(0) + " output is displayed."
            OpenCVB.intermediateReview = ""
        Else
            OpenCVB.intermediateReview = algorithm
            addendum = ""
        End If
    End Sub
    Private Sub TreeviewForm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Dim split() = Me.Text.Split()
        OpenCVB.AvailableAlgorithms.Text = split(0)
        SaveSetting("OpenCVB", "TreeViewLeft", "TreeViewLeft", Me.Left)
        SaveSetting("OpenCVB", "TreeViewTop", "TreeViewTop", Me.Top)
        SaveSetting("OpenCVB", "Accumulate", "Accumulate", Accumulate.Checked)
        OpenCVB.TreeButton.Checked = False
    End Sub
    Public Sub TreeviewForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If botDistance = 0 Then botDistance = Me.Height - ClickTreeLabel.Top
        ClickTreeLabel.Top = Me.Height - botDistance
        Accumulate.Top = ClickTreeLabel.Top + ClickTreeLabel.Height + 5
        TreeView1.Height = ClickTreeLabel.Top - 5
        PercentTime.Height = TreeView1.Height - 20
        If Me.Height < 400 Then Me.Height = 400
    End Sub
    Private Sub TreeviewForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Left = GetSetting("OpenCVB", "TreeViewLeft", "TreeViewLeft", Me.Left)
        Me.Top = GetSetting("OpenCVB", "TreeViewTop", "TreeViewTop", Me.Top)
        Accumulate.Checked = GetSetting("OpenCVB", "Accumulate", "Accumulate", True)
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
    Dim titleStr = " - Click on any node to review the algorithm's input and output."
    Public Sub updateTree()
        If OpenCVB.callTrace.Count = 0 Then Exit Sub
        moduleList.Clear()

        Dim tv = TreeView1
        tv.Nodes.Clear()
        Dim calltrace = OpenCVB.callTrace
        Dim rootcall = Trim(calltrace(0))
        Dim title = Mid(rootcall, 1, Len(rootcall) - 1)
        Me.Text = title + titleStr
        Dim n = tv.Nodes.Add(title)
        n.Tag = rootcall

        Dim entryCount = 1
        For nodeLevel = 0 To 100 ' this loop will terminate after the depth of the nesting.  100 is excessive insurance deep nesting may occur.
            Dim alldone = True
            For i = 1 To calltrace.Count - 1
                Dim fullname = calltrace(i)
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
        tv.ExpandAll()
        Me.Height = 200 + entryCount * 26
        If Me.Height > 1000 Then Me.Height = 1000 ' when too big, use the scroll bar.
        tv.HideSelection = False
        tv.SelectedNode = n
        addendum = ""
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles TreeViewTimer.Tick
        SyncLock callTraceLock
            If OpenCVB.callTrace Is Nothing Then Exit Sub
            If OpenCVB.callTrace.Count > 0 Then
                Dim firstEntry = OpenCVB.callTrace(0)
                If Len(firstEntry) Then
                    firstEntry = Mid(firstEntry, 1, Len(firstEntry) - 1)
                    If Me.Text = firstEntry + Me.titleStr = False Then Me.updateTree()
                End If
            End If
        End SyncLock
    End Sub
    Private Sub Timer1_Tick_1(sender As Object, e As EventArgs) Handles Timer1.Tick
        SyncLock callTraceLock
            Dim pTimes = OpenCVB.PercentTimes
            PercentTime.Clear()
            Dim latestPercents As New List(Of String)
            Dim latestModules As New List(Of String)
            For i = pTimes.Count - 1 To 0 Step -1
                Dim nextEntry = pTimes.ElementAt(i).Value
                Dim split = nextEntry.Split(" ")
                If moduleList.Contains(split(1)) = False Then moduleList.Add(split(1))
                latestModules.Add(split(1))
                latestPercents.Add(split(0))
            Next
            For i = 0 To moduleList.Count - 1
                If latestModules.Contains(moduleList(i)) = False Then
                    latestPercents.Add("00.0%")
                    latestModules.Add(moduleList(i))
                End If
            Next
            PercentTime.Text = "Algorithm FPS = " + Format(OpenCVB.algorithmFPS, "#0.0") + vbCrLf + vbCrLf
            For i = 0 To latestModules.Count - 1
                PercentTime.Text += latestPercents(i) + " " + latestModules(i) + vbCrLf
            Next
            PercentTime.Text += vbCrLf + "Both Algorithm and Non-Algorithm time are measured.  UI and camera task are Non-Algorithm." + vbCrLf
            PercentTime.Text += vbCrLf + vbCrLf + addendum
        End SyncLock
    End Sub
End Class
