namespace IgApiClientApplication {
    partial class DemoDotNetClientForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.loginButton = new System.Windows.Forms.Button();
            this.identifierTextbox = new System.Windows.Forms.TextBox();
            this.passwordTextbox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.restDataTextbox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.positionsButton = new System.Windows.Forms.Button();
            this.searchTextbox = new System.Windows.Forms.TextBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.activityTextbox = new System.Windows.Forms.TextBox();
            this.activity = new System.Windows.Forms.Label();
            this.streamingDataTextbox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.workingOrdersButton = new System.Windows.Forms.Button();
            this.watchlistsButton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonExit = new System.Windows.Forms.Button();
            this.btnAccountDetails = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbAccountSubscription = new System.Windows.Forms.CheckBox();
            this.cbTradeSubscription = new System.Windows.Forms.CheckBox();
            this.cbWatchlistItems = new System.Windows.Forms.CheckBox();
            this.cbOrders = new System.Windows.Forms.CheckBox();
            this.cbPositions = new System.Windows.Forms.CheckBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnMarketData = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(3, 70);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(69, 23);
            this.loginButton.TabIndex = 0;
            this.loginButton.Text = "Login";
            this.loginButton.UseVisualStyleBackColor = true;
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // identifierTextbox
            // 
            this.identifierTextbox.Location = new System.Drawing.Point(71, 9);
            this.identifierTextbox.Name = "identifierTextbox";
            this.identifierTextbox.Size = new System.Drawing.Size(125, 20);
            this.identifierTextbox.TabIndex = 1;
            // 
            // passwordTextbox
            // 
            this.passwordTextbox.Location = new System.Drawing.Point(71, 42);
            this.passwordTextbox.Name = "passwordTextbox";
            this.passwordTextbox.PasswordChar = '*';
            this.passwordTextbox.Size = new System.Drawing.Size(124, 20);
            this.passwordTextbox.TabIndex = 2;
            this.passwordTextbox.UseSystemPasswordChar = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Username";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Password";
            // 
            // restDataTextbox
            // 
            this.restDataTextbox.Location = new System.Drawing.Point(615, 37);
            this.restDataTextbox.Multiline = true;
            this.restDataTextbox.Name = "restDataTextbox";
            this.restDataTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.restDataTextbox.Size = new System.Drawing.Size(352, 586);
            this.restDataTextbox.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(692, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Rest Data";
            // 
            // positionsButton
            // 
            this.positionsButton.Location = new System.Drawing.Point(8, 92);
            this.positionsButton.Name = "positionsButton";
            this.positionsButton.Size = new System.Drawing.Size(98, 23);
            this.positionsButton.TabIndex = 11;
            this.positionsButton.Text = "Positions";
            this.positionsButton.UseVisualStyleBackColor = true;
            this.positionsButton.Click += new System.EventHandler(this.positionsButton_Click);
            // 
            // searchTextbox
            // 
            this.searchTextbox.Location = new System.Drawing.Point(112, 182);
            this.searchTextbox.Name = "searchTextbox";
            this.searchTextbox.Size = new System.Drawing.Size(97, 20);
            this.searchTextbox.TabIndex = 12;
            this.searchTextbox.Text = "voda";
            // 
            // searchButton
            // 
            this.searchButton.Location = new System.Drawing.Point(8, 179);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(98, 23);
            this.searchButton.TabIndex = 13;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
            // 
            // activityTextbox
            // 
            this.activityTextbox.Location = new System.Drawing.Point(262, 37);
            this.activityTextbox.Multiline = true;
            this.activityTextbox.Name = "activityTextbox";
            this.activityTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.activityTextbox.Size = new System.Drawing.Size(347, 587);
            this.activityTextbox.TabIndex = 14;
            // 
            // activity
            // 
            this.activity.AutoSize = true;
            this.activity.Location = new System.Drawing.Point(321, 20);
            this.activity.Name = "activity";
            this.activity.Size = new System.Drawing.Size(41, 13);
            this.activity.TabIndex = 15;
            this.activity.Text = "Activity";
            // 
            // streamingDataTextbox
            // 
            this.streamingDataTextbox.Location = new System.Drawing.Point(973, 37);
            this.streamingDataTextbox.Multiline = true;
            this.streamingDataTextbox.Name = "streamingDataTextbox";
            this.streamingDataTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.streamingDataTextbox.Size = new System.Drawing.Size(322, 587);
            this.streamingDataTextbox.TabIndex = 16;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1066, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Streaming Data";
            // 
            // workingOrdersButton
            // 
            this.workingOrdersButton.Location = new System.Drawing.Point(8, 63);
            this.workingOrdersButton.Name = "workingOrdersButton";
            this.workingOrdersButton.Size = new System.Drawing.Size(98, 23);
            this.workingOrdersButton.TabIndex = 18;
            this.workingOrdersButton.Text = "Working Orders";
            this.workingOrdersButton.UseVisualStyleBackColor = true;
            this.workingOrdersButton.Click += new System.EventHandler(this.workingOrdersButton_Click);
            // 
            // watchlistsButton
            // 
            this.watchlistsButton.Location = new System.Drawing.Point(8, 121);
            this.watchlistsButton.Name = "watchlistsButton";
            this.watchlistsButton.Size = new System.Drawing.Size(98, 23);
            this.watchlistsButton.TabIndex = 19;
            this.watchlistsButton.Text = "Watchlists";
            this.watchlistsButton.UseVisualStyleBackColor = true;
            this.watchlistsButton.Click += new System.EventHandler(this.watchlistsButton_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 4);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(160, 13);
            this.label6.TabIndex = 21;
            this.label6.Text = "Dynamic Data from Lighstreamer";
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(150, 70);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(72, 23);
            this.buttonExit.TabIndex = 22;
            this.buttonExit.Text = "Exit";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // btnAccountDetails
            // 
            this.btnAccountDetails.Location = new System.Drawing.Point(8, 34);
            this.btnAccountDetails.Name = "btnAccountDetails";
            this.btnAccountDetails.Size = new System.Drawing.Size(96, 23);
            this.btnAccountDetails.TabIndex = 24;
            this.btnAccountDetails.Text = "Account Details";
            this.btnAccountDetails.UseVisualStyleBackColor = true;
            this.btnAccountDetails.Click += new System.EventHandler(this.btnAccountDetails_Click);
            // 
            // btnLogout
            // 
            this.btnLogout.Location = new System.Drawing.Point(78, 70);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(69, 23);
            this.btnLogout.TabIndex = 26;
            this.btnLogout.Text = "Logout";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.cbAccountSubscription);
            this.panel1.Controls.Add(this.cbTradeSubscription);
            this.panel1.Controls.Add(this.cbWatchlistItems);
            this.panel1.Controls.Add(this.cbOrders);
            this.panel1.Controls.Add(this.cbPositions);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Location = new System.Drawing.Point(25, 396);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(231, 182);
            this.panel1.TabIndex = 29;
            // 
            // cbAccountSubscription
            // 
            this.cbAccountSubscription.AutoSize = true;
            this.cbAccountSubscription.Location = new System.Drawing.Point(29, 34);
            this.cbAccountSubscription.Name = "cbAccountSubscription";
            this.cbAccountSubscription.Size = new System.Drawing.Size(115, 17);
            this.cbAccountSubscription.TabIndex = 44;
            this.cbAccountSubscription.Text = "Live Account Data";
            this.cbAccountSubscription.UseVisualStyleBackColor = true;
            this.cbAccountSubscription.CheckedChanged += new System.EventHandler(this.cbAccountSubscription_CheckedChanged);
            // 
            // cbTradeSubscription
            // 
            this.cbTradeSubscription.AutoSize = true;
            this.cbTradeSubscription.Location = new System.Drawing.Point(29, 57);
            this.cbTradeSubscription.Name = "cbTradeSubscription";
            this.cbTradeSubscription.Size = new System.Drawing.Size(164, 17);
            this.cbTradeSubscription.TabIndex = 43;
            this.cbTradeSubscription.Text = "Live Trade Subscription Data";
            this.cbTradeSubscription.UseVisualStyleBackColor = true;
            this.cbTradeSubscription.CheckedChanged += new System.EventHandler(this.cbTradeSubscription_CheckedChanged);
            // 
            // cbWatchlistItems
            // 
            this.cbWatchlistItems.AutoSize = true;
            this.cbWatchlistItems.Location = new System.Drawing.Point(29, 126);
            this.cbWatchlistItems.Name = "cbWatchlistItems";
            this.cbWatchlistItems.Size = new System.Drawing.Size(159, 17);
            this.cbWatchlistItems.TabIndex = 42;
            this.cbWatchlistItems.Text = "Live Watchlist Item Updates";
            this.cbWatchlistItems.UseVisualStyleBackColor = true;
            this.cbWatchlistItems.CheckedChanged += new System.EventHandler(this.cbWatchlistItems_CheckedChanged);
            // 
            // cbOrders
            // 
            this.cbOrders.AutoSize = true;
            this.cbOrders.Location = new System.Drawing.Point(29, 103);
            this.cbOrders.Name = "cbOrders";
            this.cbOrders.Size = new System.Drawing.Size(123, 17);
            this.cbOrders.TabIndex = 41;
            this.cbOrders.Text = "Live Orders Updates";
            this.cbOrders.UseVisualStyleBackColor = true;
            this.cbOrders.CheckedChanged += new System.EventHandler(this.cbOrders_CheckedChanged);
            // 
            // cbPositions
            // 
            this.cbPositions.AutoSize = true;
            this.cbPositions.Location = new System.Drawing.Point(29, 80);
            this.cbPositions.Name = "cbPositions";
            this.cbPositions.Size = new System.Drawing.Size(134, 17);
            this.cbPositions.TabIndex = 40;
            this.cbPositions.Text = "Live Positions Updates";
            this.cbPositions.UseVisualStyleBackColor = true;
            this.cbPositions.CheckedChanged += new System.EventHandler(this.cbPositions_CheckedChanged);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.btnMarketData);
            this.panel2.Controls.Add(this.label7);
            this.panel2.Controls.Add(this.workingOrdersButton);
            this.panel2.Controls.Add(this.positionsButton);
            this.panel2.Controls.Add(this.watchlistsButton);
            this.panel2.Controls.Add(this.btnAccountDetails);
            this.panel2.Controls.Add(this.searchButton);
            this.panel2.Controls.Add(this.searchTextbox);
            this.panel2.Location = new System.Drawing.Point(25, 172);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(231, 218);
            this.panel2.TabIndex = 30;
            // 
            // btnMarketData
            // 
            this.btnMarketData.Location = new System.Drawing.Point(8, 150);
            this.btnMarketData.Name = "btnMarketData";
            this.btnMarketData.Size = new System.Drawing.Size(98, 23);
            this.btnMarketData.TabIndex = 27;
            this.btnMarketData.Text = "Market Data";
            this.btnMarketData.UseVisualStyleBackColor = true;
            this.btnMarketData.Click += new System.EventHandler(this.btnMarketData_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 9);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(108, 13);
            this.label7.TabIndex = 26;
            this.label7.Text = "Static Data from Rest";
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel3.Controls.Add(this.identifierTextbox);
            this.panel3.Controls.Add(this.passwordTextbox);
            this.panel3.Controls.Add(this.label1);
            this.panel3.Controls.Add(this.buttonExit);
            this.panel3.Controls.Add(this.btnLogout);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Controls.Add(this.loginButton);
            this.panel3.Location = new System.Drawing.Point(25, 37);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(231, 109);
            this.panel3.TabIndex = 31;
            // 
            // DemoDotNetClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1322, 635);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.streamingDataTextbox);
            this.Controls.Add(this.activity);
            this.Controls.Add(this.activityTextbox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.restDataTextbox);
            this.Name = "DemoDotNetClientForm";
            this.Text = "WinForm Trader - IG Reference Application";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DemoDotNetClientForm_FormClosed);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.TextBox identifierTextbox;
        private System.Windows.Forms.TextBox passwordTextbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox restDataTextbox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button positionsButton;
        private System.Windows.Forms.TextBox searchTextbox;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.TextBox activityTextbox;
        private System.Windows.Forms.Label activity;
        private System.Windows.Forms.TextBox streamingDataTextbox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button workingOrdersButton;
        private System.Windows.Forms.Button watchlistsButton;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Button btnAccountDetails;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbPositions;
        private System.Windows.Forms.CheckBox cbWatchlistItems;
        private System.Windows.Forms.CheckBox cbOrders;
        private System.Windows.Forms.Button btnMarketData;
        private System.Windows.Forms.CheckBox cbTradeSubscription;
        private System.Windows.Forms.CheckBox cbAccountSubscription;
        private System.Windows.Forms.Panel panel3;
    }
}