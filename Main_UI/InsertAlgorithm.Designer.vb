<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class InsertAlgorithm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing And components IsNot Nothing Then
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
        Me.AddOpenGL = New System.Windows.Forms.Button()
        Me.Add_AI_Generated = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.AddCPP = New System.Windows.Forms.Button()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.AddCSharp = New System.Windows.Forms.Button()
        Me.Label7 = New System.Windows.Forms.Label()
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
        Me.Label1.Size = New System.Drawing.Size(658, 20)
        Me.Label1.TabIndex = 4
        Me.Label1.Text = "Algorithm Name - must have the form of 'ModuleName_ClassName', i.e. RedColor_Basics" &
    "cscs"
        '
        'AddVB
        '
        Me.AddVB.Location = New System.Drawing.Point(18, 135)
        Me.AddVB.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddVB.Name = "AddVB"
        Me.AddVB.Size = New System.Drawing.Size(330, 38)
        Me.AddVB.TabIndex = 5
        Me.AddVB.Text = "Add VB.Net Algorithm"
        Me.AddVB.UseVisualStyleBackColor = True
        '
        'AddOpenGL
        '
        Me.AddOpenGL.Location = New System.Drawing.Point(18, 213)
        Me.AddOpenGL.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddOpenGL.Name = "AddOpenGL"
        Me.AddOpenGL.Size = New System.Drawing.Size(330, 38)
        Me.AddOpenGL.TabIndex = 7
        Me.AddOpenGL.Text = "Add OpenGL Algorithm"
        Me.AddOpenGL.UseVisualStyleBackColor = True
        '
        'Add_AI_Generated
        '
        Me.Add_AI_Generated.Location = New System.Drawing.Point(18, 301)
        Me.Add_AI_Generated.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Add_AI_Generated.Name = "Add_AI_Generated"
        Me.Add_AI_Generated.Size = New System.Drawing.Size(330, 38)
        Me.Add_AI_Generated.TabIndex = 10
        Me.Add_AI_Generated.Text = "Add Managed AI-Generated C++ Algorithm"
        Me.Add_AI_Generated.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(25, 113)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(386, 20)
        Me.Label2.TabIndex = 11
        Me.Label2.Text = "Enter above 'Edge_Canny', appears as 'Edge_Canny'"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(25, 276)
        Me.Label3.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(426, 20)
        Me.Label3.TabIndex = 12
        Me.Label3.Text = "Enter above 'Edge_Canny', appears as 'Edge_Canny_CPP'"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(25, 188)
        Me.Label5.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(456, 20)
        Me.Label5.TabIndex = 14
        Me.Label5.Text = "Enter above 'Edge_Canny', appears as 'OpenGL_Edge_Canny'"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(25, 360)
        Me.Label4.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(426, 20)
        Me.Label4.TabIndex = 13
        Me.Label4.Text = "Enter above 'Edge_Canny', appears as 'Edge_Canny_CPP'"
        Me.Label4.Visible = False
        '
        'AddCPP
        '
        Me.AddCPP.Location = New System.Drawing.Point(18, 385)
        Me.AddCPP.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddCPP.Name = "AddCPP"
        Me.AddCPP.Size = New System.Drawing.Size(330, 38)
        Me.AddCPP.TabIndex = 8
        Me.AddCPP.Text = "Add Unmanaged (Native) C++ Algorithm (Old Style)"
        Me.AddCPP.UseVisualStyleBackColor = True
        Me.AddCPP.Visible = False
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(25, 439)
        Me.Label6.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(417, 20)
        Me.Label6.TabIndex = 15
        Me.Label6.Text = "Enter above 'Edge_Canny', appears as 'Edge_Canny_CS'"
        Me.Label6.Visible = False
        '
        'AddCSharp
        '
        Me.AddCSharp.Location = New System.Drawing.Point(18, 464)
        Me.AddCSharp.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddCSharp.Name = "AddCSharp"
        Me.AddCSharp.Size = New System.Drawing.Size(330, 38)
        Me.AddCSharp.TabIndex = 6
        Me.AddCSharp.Text = "Add C# Algorithm"
        Me.AddCSharp.UseVisualStyleBackColor = True
        Me.AddCSharp.Visible = False
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(25, 534)
        Me.Label7.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(436, 20)
        Me.Label7.TabIndex = 16
        Me.Label7.Text = "Enter above 'Edge_Canny', appears as 'Edge_Canny_PS.py'"
        Me.Label7.Visible = False
        '
        'AddPyStream
        '
        Me.AddPyStream.Location = New System.Drawing.Point(18, 559)
        Me.AddPyStream.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.AddPyStream.Name = "AddPyStream"
        Me.AddPyStream.Size = New System.Drawing.Size(330, 38)
        Me.AddPyStream.TabIndex = 9
        Me.AddPyStream.Text = "Add PyStream Algorithm"
        Me.AddPyStream.UseVisualStyleBackColor = True
        Me.AddPyStream.Visible = False
        '
        'InsertAlgorithm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(824, 611)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Add_AI_Generated)
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
    Friend WithEvents AddOpenGL As Button
    Friend WithEvents Add_AI_Generated As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents AddCPP As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents AddCSharp As Button
    Friend WithEvents Label7 As Label
    Friend WithEvents AddPyStream As Button
End Class
