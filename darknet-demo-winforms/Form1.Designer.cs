
namespace darknet_demo_winforms
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label1 = new System.Windows.Forms.Label();
            this.darkModeCheckbox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(12, 201);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(768, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Try changing your color mode in Windows Settings to see this app\'s title bar and " +
    "menu react.";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // darkModeCheckbox
            // 
            this.darkModeCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.darkModeCheckbox.AutoSize = true;
            this.darkModeCheckbox.Checked = true;
            this.darkModeCheckbox.CheckState = System.Windows.Forms.CheckState.Indeterminate;
            this.darkModeCheckbox.Location = new System.Drawing.Point(359, 257);
            this.darkModeCheckbox.Name = "darkModeCheckbox";
            this.darkModeCheckbox.Size = new System.Drawing.Size(78, 17);
            this.darkModeCheckbox.TabIndex = 1;
            this.darkModeCheckbox.Text = "Dark mode";
            this.darkModeCheckbox.ThreeState = true;
            this.darkModeCheckbox.UseVisualStyleBackColor = false;
            this.darkModeCheckbox.CheckStateChanged += new System.EventHandler(this.onDarkModeCheckboxChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(19)))), ((int)(((byte)(19)))), ((int)(((byte)(19)))));
            this.ClientSize = new System.Drawing.Size(792, 419);
            this.Controls.Add(this.darkModeCheckbox);
            this.Controls.Add(this.label1);
            this.ForeColor = System.Drawing.Color.White;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Forms demo - .NET Framework 4.8 - DarkNet";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox darkModeCheckbox;
    }
}

