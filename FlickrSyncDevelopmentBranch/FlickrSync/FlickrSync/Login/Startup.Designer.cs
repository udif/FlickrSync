namespace FlickrSync
{
    partial class Startup
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.welcomeLabel = new System.Windows.Forms.Label();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.preferencesButton = new System.Windows.Forms.Button();
            this.choiceLabel = new System.Windows.Forms.Label();
            this.flickrButton = new System.Windows.Forms.Button();
            this.preferencesLabel = new System.Windows.Forms.Label();
            this.noteLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67F));
            this.tableLayoutPanel.Controls.Add(this.welcomeLabel, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.logoPictureBox, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.progressBar, 1, 6);
            this.tableLayoutPanel.Controls.Add(this.preferencesButton, 1, 5);
            this.tableLayoutPanel.Controls.Add(this.choiceLabel, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.flickrButton, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.preferencesLabel, 1, 4);
            this.tableLayoutPanel.Controls.Add(this.noteLabel, 1, 3);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 7;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(520, 266);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // welcomeLabel
            // 
            this.welcomeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.welcomeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F);
            this.welcomeLabel.Location = new System.Drawing.Point(177, 0);
            this.welcomeLabel.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.welcomeLabel.MaximumSize = new System.Drawing.Size(0, 50);
            this.welcomeLabel.Name = "welcomeLabel";
            this.welcomeLabel.Size = new System.Drawing.Size(340, 50);
            this.welcomeLabel.TabIndex = 20;
            this.welcomeLabel.Text = "Welcome to FlickrSync";
            this.welcomeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logoPictureBox.Image = global::FlickrSync.Properties.Resources.Logo;
            this.logoPictureBox.Location = new System.Drawing.Point(3, 3);
            this.logoPictureBox.Name = "logoPictureBox";
            this.tableLayoutPanel.SetRowSpan(this.logoPictureBox, 7);
            this.logoPictureBox.Size = new System.Drawing.Size(165, 260);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.logoPictureBox.TabIndex = 12;
            this.logoPictureBox.TabStop = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(174, 239);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(343, 23);
            this.progressBar.TabIndex = 13;
            // 
            // preferencesButton
            // 
            this.preferencesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.preferencesButton.Location = new System.Drawing.Point(412, 201);
            this.preferencesButton.Name = "preferencesButton";
            this.preferencesButton.Size = new System.Drawing.Size(105, 32);
            this.preferencesButton.TabIndex = 15;
            this.preferencesButton.Text = "Preferences";
            this.preferencesButton.UseVisualStyleBackColor = true;
            this.preferencesButton.Click += new System.EventHandler(this.preferencesButton_Click);
            // 
            // choiceLabel
            // 
            this.choiceLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.choiceLabel.AutoSize = true;
            this.choiceLabel.Location = new System.Drawing.Point(228, 53);
            this.choiceLabel.Name = "choiceLabel";
            this.choiceLabel.Size = new System.Drawing.Size(235, 13);
            this.choiceLabel.TabIndex = 21;
            this.choiceLabel.Text = "Please select your online photo service provider:";
            // 
            // flickrButton
            // 
            this.flickrButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flickrButton.Location = new System.Drawing.Point(298, 82);
            this.flickrButton.Name = "flickrButton";
            this.flickrButton.Size = new System.Drawing.Size(95, 37);
            this.flickrButton.TabIndex = 22;
            this.flickrButton.Text = "Flickr";
            this.flickrButton.UseVisualStyleBackColor = true;
            this.flickrButton.Click += new System.EventHandler(this.flickrButton_Click);
            // 
            // preferencesLabel
            // 
            this.preferencesLabel.AutoSize = true;
            this.preferencesLabel.Location = new System.Drawing.Point(174, 171);
            this.preferencesLabel.Name = "preferencesLabel";
            this.preferencesLabel.Size = new System.Drawing.Size(314, 13);
            this.preferencesLabel.TabIndex = 23;
            this.preferencesLabel.Text = "Please click on the Preferences button if you need to use a proxy";
            // 
            // noteLabel
            // 
            this.noteLabel.AutoSize = true;
            this.noteLabel.Location = new System.Drawing.Point(174, 132);
            this.noteLabel.Name = "noteLabel";
            this.noteLabel.Size = new System.Drawing.Size(303, 13);
            this.noteLabel.TabIndex = 24;
            this.noteLabel.Text = "Note: Flickr is currently the only available service for FlickrSync";
            // 
            // Startup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(538, 284);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Startup";
            this.Padding = new System.Windows.Forms.Padding(9);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Startup";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Startup_Closed);
            this.Load += new System.EventHandler(this.Startup_Load);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button preferencesButton;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Label welcomeLabel;
        private System.Windows.Forms.Label choiceLabel;
        private System.Windows.Forms.Button flickrButton;
        private System.Windows.Forms.Label preferencesLabel;
        private System.Windows.Forms.Label noteLabel;

    }
}
