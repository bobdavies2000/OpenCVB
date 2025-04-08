<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsFeatures
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
        Me.FeaturesGroup = New System.Windows.Forms.GroupBox()
        Me.LineGroup = New System.Windows.Forms.GroupBox()
        Me.MaskGroup = New System.Windows.Forms.GroupBox()
        Me.FeatureMethods = New System.Windows.Forms.GroupBox()
        Me.RadioButton1 = New System.Windows.Forms.RadioButton()
        Me.RadioButton2 = New System.Windows.Forms.RadioButton()
        Me.RadioButton3 = New System.Windows.Forms.RadioButton()
        Me.RadioButton4 = New System.Windows.Forms.RadioButton()
        Me.RadioButton5 = New System.Windows.Forms.RadioButton()
        Me.RadioButton6 = New System.Windows.Forms.RadioButton()
        Me.FeaturesGroup.SuspendLayout()
        Me.FeatureMethods.SuspendLayout()
        Me.SuspendLayout()
        '
        'FeaturesGroup
        '
        Me.FeaturesGroup.Controls.Add(Me.FeatureMethods)
        Me.FeaturesGroup.Location = New System.Drawing.Point(28, 12)
        Me.FeaturesGroup.Name = "FeaturesGroup"
        Me.FeaturesGroup.Size = New System.Drawing.Size(328, 568)
        Me.FeaturesGroup.TabIndex = 0
        Me.FeaturesGroup.TabStop = False
        Me.FeaturesGroup.Text = "Features"
        '
        'LineGroup
        '
        Me.LineGroup.Location = New System.Drawing.Point(381, 12)
        Me.LineGroup.Name = "LineGroup"
        Me.LineGroup.Size = New System.Drawing.Size(328, 568)
        Me.LineGroup.TabIndex = 1
        Me.LineGroup.TabStop = False
        Me.LineGroup.Text = "Lines"
        '
        'MaskGroup
        '
        Me.MaskGroup.Location = New System.Drawing.Point(738, 21)
        Me.MaskGroup.Name = "MaskGroup"
        Me.MaskGroup.Size = New System.Drawing.Size(328, 568)
        Me.MaskGroup.TabIndex = 2
        Me.MaskGroup.TabStop = False
        Me.MaskGroup.Text = "Masks"
        '
        'FeatureMethods
        '
        Me.FeatureMethods.Controls.Add(Me.RadioButton6)
        Me.FeatureMethods.Controls.Add(Me.RadioButton5)
        Me.FeatureMethods.Controls.Add(Me.RadioButton4)
        Me.FeatureMethods.Controls.Add(Me.RadioButton3)
        Me.FeatureMethods.Controls.Add(Me.RadioButton2)
        Me.FeatureMethods.Controls.Add(Me.RadioButton1)
        Me.FeatureMethods.Location = New System.Drawing.Point(6, 34)
        Me.FeatureMethods.Name = "FeatureMethods"
        Me.FeatureMethods.Size = New System.Drawing.Size(263, 190)
        Me.FeatureMethods.TabIndex = 3
        Me.FeatureMethods.TabStop = False
        Me.FeatureMethods.Text = "Method"
        '
        'RadioButton1
        '
        Me.RadioButton1.AutoSize = True
        Me.RadioButton1.Location = New System.Drawing.Point(17, 30)
        Me.RadioButton1.Name = "RadioButton1"
        Me.RadioButton1.Size = New System.Drawing.Size(220, 24)
        Me.RadioButton1.TabIndex = 0
        Me.RadioButton1.TabStop = True
        Me.RadioButton1.Text = "Good Features Full Image"
        Me.RadioButton1.UseVisualStyleBackColor = True
        '
        'RadioButton2
        '
        Me.RadioButton2.AutoSize = True
        Me.RadioButton2.Location = New System.Drawing.Point(17, 54)
        Me.RadioButton2.Name = "RadioButton2"
        Me.RadioButton2.Size = New System.Drawing.Size(176, 24)
        Me.RadioButton2.TabIndex = 1
        Me.RadioButton2.TabStop = True
        Me.RadioButton2.Text = "Good Features Grid"
        Me.RadioButton2.UseVisualStyleBackColor = True
        '
        'RadioButton3
        '
        Me.RadioButton3.AutoSize = True
        Me.RadioButton3.Location = New System.Drawing.Point(17, 78)
        Me.RadioButton3.Name = "RadioButton3"
        Me.RadioButton3.Size = New System.Drawing.Size(89, 24)
        Me.RadioButton3.TabIndex = 2
        Me.RadioButton3.TabStop = True
        Me.RadioButton3.Text = "AGAST"
        Me.RadioButton3.UseVisualStyleBackColor = True
        '
        'RadioButton4
        '
        Me.RadioButton4.AutoSize = True
        Me.RadioButton4.Location = New System.Drawing.Point(17, 102)
        Me.RadioButton4.Name = "RadioButton4"
        Me.RadioButton4.Size = New System.Drawing.Size(83, 24)
        Me.RadioButton4.TabIndex = 3
        Me.RadioButton4.TabStop = True
        Me.RadioButton4.Text = "BRISK"
        Me.RadioButton4.UseVisualStyleBackColor = True
        '
        'RadioButton5
        '
        Me.RadioButton5.AutoSize = True
        Me.RadioButton5.Location = New System.Drawing.Point(17, 126)
        Me.RadioButton5.Name = "RadioButton5"
        Me.RadioButton5.Size = New System.Drawing.Size(76, 24)
        Me.RadioButton5.TabIndex = 4
        Me.RadioButton5.TabStop = True
        Me.RadioButton5.Text = "Harris"
        Me.RadioButton5.UseVisualStyleBackColor = True
        '
        'RadioButton6
        '
        Me.RadioButton6.AutoSize = True
        Me.RadioButton6.Location = New System.Drawing.Point(17, 150)
        Me.RadioButton6.Name = "RadioButton6"
        Me.RadioButton6.Size = New System.Drawing.Size(75, 24)
        Me.RadioButton6.TabIndex = 5
        Me.RadioButton6.TabStop = True
        Me.RadioButton6.Text = "FAST"
        Me.RadioButton6.UseVisualStyleBackColor = True
        '
        'OptionsFeatures
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1419, 616)
        Me.Controls.Add(Me.MaskGroup)
        Me.Controls.Add(Me.LineGroup)
        Me.Controls.Add(Me.FeaturesGroup)
        Me.Name = "OptionsFeatures"
        Me.Text = "OptionsFeatures"
        Me.FeaturesGroup.ResumeLayout(False)
        Me.FeatureMethods.ResumeLayout(False)
        Me.FeatureMethods.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents FeaturesGroup As Windows.Forms.GroupBox
    Friend WithEvents FeatureMethods As Windows.Forms.GroupBox
    Friend WithEvents LineGroup As Windows.Forms.GroupBox
    Friend WithEvents MaskGroup As Windows.Forms.GroupBox
    Friend WithEvents RadioButton6 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton5 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton4 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton3 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton2 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton1 As Windows.Forms.RadioButton
End Class
