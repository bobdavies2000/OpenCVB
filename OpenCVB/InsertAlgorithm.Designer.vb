<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class InsertAlgorithm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.AlgorithmName = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.AddVB = New System.Windows.Forms.Button()
        Me.AddCSharp = New System.Windows.Forms.Button()
        Me.AddOpenGL = New System.Windows.Forms.Button()
        Me.AddCPP = New System.Windows.Forms.Button()
        Me.AddPyStream = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'AlgorithmName
        '
        Me.AlgorithmName.Location = New System.Drawing.Point(18, 46)
        Me.AlgorithmName.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AlgorithmName.Name = "AlgorithmName"
        Me.AlgorithmName.Size = New System.Drawing.Size(499, 26)
        Me.AlgorithmName.TabIndex = 3
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(14, 22)
        Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(642, 20)
        Me.Label1.TabIndex = 4
        Me.Label1.Text = "Algorithm Name - must have the form of 'ModuleName_ClassName', i.e. RedCloud_Basi" &
    "cs"
        '
        'AddVB
        '
        Me.AddVB.Location = New System.Drawing.Point(18, 86)
        Me.AddVB.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddVB.Name = "AddVB"
        Me.AddVB.Size = New System.Drawing.Size(330, 38)
        Me.AddVB.TabIndex = 5
        Me.AddVB.Text = "Add VB.Net Algorithm"
        Me.AddVB.UseVisualStyleBackColor = True
        '
        'AddCSharp
        '
        Me.AddCSharp.Location = New System.Drawing.Point(18, 229)
        Me.AddCSharp.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddCSharp.Name = "AddCSharp"
        Me.AddCSharp.Size = New System.Drawing.Size(330, 38)
        Me.AddCSharp.TabIndex = 6
        Me.AddCSharp.Text = "Add C# Algorithm"
        Me.AddCSharp.UseVisualStyleBackColor = True
        '
        'AddOpenGL
        '
        Me.AddOpenGL.Location = New System.Drawing.Point(18, 182)
        Me.AddOpenGL.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddOpenGL.Name = "AddOpenGL"
        Me.AddOpenGL.Size = New System.Drawing.Size(330, 38)
        Me.AddOpenGL.TabIndex = 7
        Me.AddOpenGL.Text = "Add OpenGL Algorithm"
        Me.AddOpenGL.UseVisualStyleBackColor = True
        '
        'AddCPP
        '
        Me.AddCPP.Location = New System.Drawing.Point(18, 134)
        Me.AddCPP.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddCPP.Name = "AddCPP"
        Me.AddCPP.Size = New System.Drawing.Size(330, 38)
        Me.AddCPP.TabIndex = 8
        Me.AddCPP.Text = "Add C++ Algorithm"
        Me.AddCPP.UseVisualStyleBackColor = True
        '
        'AddPyStream
        '
        Me.AddPyStream.Location = New System.Drawing.Point(18, 277)
        Me.AddPyStream.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddPyStream.Name = "AddPyStream"
        Me.AddPyStream.Size = New System.Drawing.Size(330, 38)
        Me.AddPyStream.TabIndex = 9
        Me.AddPyStream.Text = "Add PyStream Algorithm"
        Me.AddPyStream.UseVisualStyleBackColor = True
        '
        'InsertAlgorithm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(824, 348)
        Me.Controls.Add(Me.AddPyStream)
        Me.Controls.Add(Me.AddCPP)
        Me.Controls.Add(Me.AddOpenGL)
        Me.Controls.Add(Me.AddCSharp)
        Me.Controls.Add(Me.AddVB)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.AlgorithmName)
        Me.KeyPreview = True
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Name = "InsertAlgorithm"
        Me.Text = "Add Algorithm"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents AlgorithmName As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents AddVB As Button
    Friend WithEvents AddCSharp As Button
    Friend WithEvents AddOpenGL As Button
    Friend WithEvents AddCPP As Button
    Friend WithEvents AddPyStream As Button
End Class
