using CS_Classes;
using OpenCvSharp.Flann;
using System.Xml.Linq;

namespace CS_Classes
{
    partial class OptionsCheckBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        bool setup(string traceName)
        {
            //if (findfrm(traceName + " CheckBoxes") != null) return false;
            //this.MdiParent = allOptions;
            //this.Text = traceName + " CheckBoxes";
            //allOptions.addTitle(this);
            //this.show();
            return true;
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // OptionsCheckBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 237);
            this.Name = "OptionsCheckBox";
            this.Text = "OptionsCheckBox";
            this.ResumeLayout(false);

        }

        #endregion
    }
}




//class OptionsCheckbox
//{
//    constructor()
//    {
//        this.Box = [];
//    }

//    setup(traceName)
//    {
//        if (findfrm(traceName + " CheckBoxes") !== null) return false;
//        this.MdiParent = allOptions;
//        this.Text = traceName + " CheckBoxes";
//        allOptions.addTitle(this);
//        this.show();
//        return true;
//    }

//    addCheckBox(labelStr)
//    {
//        const index = this.Box.length;
//        const checkBox = document.createElement('input');
//        checkBox.type = 'checkbox';
//        checkBox.addEventListener('change', this.boxCheckChanged.bind(this));
//        checkBox.style.display = 'inline-block';
//        checkBox.textContent = labelStr;
//        this.Box.push(checkBox);
//        document.getElementById('FlowLayoutPanel1').appendChild(checkBox);
//    }

//    boxCheckChanged(event) {
//        task.optionsChanged = true;
//    }

//    optionsCheckboxClick(event) {
//        this.bringToFront();
//    }

//    flowLayoutPanel1Click(event) {
//        this.bringToFront();
//    }

//    bringToFront() {
//        // Implement bring to front logic
//    }

//show() {
//    // Implement show logic
//}
//}

//// Event listeners for the click events
//document.getElementById('OptionsCheckbox').addEventListener('click', function(event) {
//    const optionsCheckbox = new OptionsCheckbox();
//    optionsCheckbox.optionsCheckboxClick(event);
//});

//document.getElementById('FlowLayoutPanel1').addEventListener('click', function(event) {
//    const optionsCheckbox = new OptionsCheckbox();
//    optionsCheckbox.flowLayoutPanel1Click(event);
//});
