﻿Imports System.ComponentModel
Imports cv = OpenCvSharp
Public Class TreeviewForm
    Dim botDistance As Integer
    Dim treeData As New List(Of String)
    Dim moduleList As New List(Of String) ' the list of all active algorithms.
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        Me.TreeViewTimer.Enabled = False
        Dim algorithm = e.Node.Text
        Dim split = e.Node.Text.Split(" ")
        OpenCVB.intermediateReview = split(0)
    End Sub
    Private Sub TreeviewForm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Dim split() = Me.Text.Split()
        If split(0) <> "CPP_Basics" Then OpenCVB.AvailableAlgorithms.Text = split(0)
    End Sub
    Public Sub TreeviewForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        TreeView1.Height = Me.Height
        PercentTime.Height = TreeView1.Height
    End Sub
    Private Sub TreeviewForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Left = GetSetting("OpenCVB", "treeViewLeft", "treeViewLeft", Me.Left)
        Me.Top = GetSetting("OpenCVB", "treeViewTop", "treeViewTop", Me.Top)
        Me.Width = GetSetting("OpenCVB", "treeViewWidth", "treeViewWidth", Me.Width)
        Me.Height = GetSetting("OpenCVB", "treeViewHeight", "treeViewHeight", Me.Height)
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
    Private Sub Options_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        SaveSetting("OpenCVB", "treeViewLeft", "treeViewLeft", Me.Left)
        SaveSetting("OpenCVB", "treeViewTop", "treeViewTop", Me.Top)
        SaveSetting("OpenCVB", "treeViewWidth", "treeViewWidth", Me.Width)
        SaveSetting("OpenCVB", "treeViewHeight", "treeViewHeight", Me.Height)
    End Sub
    Private Function getNode(tv As TreeView, name As String) As TreeNode
        For Each n In tv.Nodes
            If n.tag = name Then Return n
            Dim rnode = FindRecursive(n, name)
            If rnode IsNot Nothing Then Return rnode
        Next
        Return Nothing
    End Function
    Dim titleStr = " - Click on any node to review the algorithm's output."
    Public Sub updateTree()
        If OpenCVB.callTrace.Count = 0 Then Exit Sub
        moduleList.Clear()

        Dim tv = TreeView1
        tv.Nodes.Clear()
        Dim callTrace = OpenCVB.callTrace
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
            Dim split() = sn.Split("\")
            treeData.Add(split(split.Length - 2))
        Next

        tv.ExpandAll()
        tv.HideSelection = False
        tv.SelectedNode = n
    End Sub
    Private Sub TreeView_Tick(sender As Object, e As EventArgs) Handles TreeViewTimer.Tick
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
    Private Class compareAllowIdenticalSingle : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If OpenCVB.testAllRunning Then Exit Sub ' may not be enough time to report valid statistics.
        SyncLock callTraceLock
            If OpenCVB.testAllRunning = False Then
                Dim algorithm_ms = New List(Of Single)(OpenCVB.algorithm_ms)
                If algorithm_ms.Count = 0 Then
                    PercentTime.Text = ""
                    Exit Sub
                End If
                Dim PercentTimes As New SortedList(Of Single, String)(New compareAllowIdenticalSingle)
                Dim sumTime As Single
                For i = 0 To algorithm_ms.Count - 1
                    sumTime += algorithm_ms(i)
                Next

                Dim saveWaitTime As String = ""

                For i = 0 To algorithm_ms.Count - 1
                    Dim percent = algorithm_ms(i) / sumTime
                    If percent < 0 Then percent = 0
                    Dim str = Format(percent, "00.0%") + " " + OpenCVB.algorithmNames(i)
                    If OpenCVB.algorithmNames(i).Contains("waitingForInput") Then
                        str += "  <<<<<<<<<< "
                        saveWaitTime = str
                    End If
                    PercentTimes.Add(percent, str)
                Next

                PercentTime.Text = ""
                PercentTime.Text = "Algorithm FPS = " + Format(OpenCVB.algorithmFPS, "0.0") + vbCrLf
                PercentTime.Text += "% = function times for algorithm task only." + vbCrLf + vbCrLf
                Static boldFont = New Font(PercentTime.Font, FontStyle.Bold)
                Static regularFont = New Font(PercentTime.Font, FontStyle.Regular)

                Dim timeDataTree As New List(Of String)(treeData)
                For i = 0 To PercentTimes.Count - 1
                    Dim str = PercentTimes.ElementAt(i).Value
                    Dim index = treeData.IndexOf(str.Substring(6))
                    PercentTime.Text += str + vbCrLf
                    If index >= 0 Then timeDataTree(index) = str.Substring(0, 5) + " " + timeDataTree(index)
                Next

                PercentTime.Text += "---------------- Tree order display: " + vbCrLf
                PercentTime.Text += saveWaitTime + vbCrLf
                For Each sn In timeDataTree
                    If sn.Contains("%") Then PercentTime.Text += sn + vbCrLf
                Next

            End If
        End SyncLock
    End Sub
End Class
