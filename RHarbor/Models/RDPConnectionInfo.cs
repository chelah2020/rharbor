﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace kenzauros.RHarbor.Models
{
    [Serializable]
    [CategoryOrder("General", 1)]
    [CategoryOrder("Remote", 2)]
    [CategoryOrder("Authentication", 3)]
    [CategoryOrder("Screen", 5)]
    [CategoryOrder("Other", 7)]
    [Table("rdp_connection_infos")]
    internal class RDPConnectionInfo : ConnectionInfoBase
    {
        #region Static

        private static readonly RDPConnectionInfo Empty = new RDPConnectionInfo();

        #endregion

        [Required]
        [Category("Screen"), PropertyOrder(1)]
        public bool FullScreen { get { return _FullScreen; } set { SetProp(ref _FullScreen, value); } }
        private bool _FullScreen = false;

        [Category("Screen"), PropertyOrder(3), DisplayName("Desktop Width"), Editor(typeof(IntegerUpDownEditor), typeof(IntegerUpDownEditor))]
        public int? DesktopWidth { get { return _DesktopWidth; } set { SetProp(ref _DesktopWidth, value); RaisePropertyChanged(nameof(DesktopResulution)); } }
        private int? _DesktopWidth;

        [Category("Screen"), PropertyOrder(4), DisplayName("Desktop Height"), Editor(typeof(IntegerUpDownEditor), typeof(IntegerUpDownEditor))]
        public int? DesktopHeight { get { return _DesktopHeight; } set { SetProp(ref _DesktopHeight, value); RaisePropertyChanged(nameof(DesktopResulution)); } }
        private int? _DesktopHeight;

        [Required]
        [Category("Other"), PropertyOrder(1), DisplayName("Admin mode")]
        public bool Admin { get { return _Admin; } set { SetProp(ref _Admin, value); } }
        private bool _Admin = false;

        [Browsable(false)]
        public string Settings { get { return _Settings; } set { SetProp(ref _Settings, value); } }
        private string _Settings = "";

        [ForeignKey("RequiredConnection")]
        [Category("Other"), PropertyOrder(2), DisplayName("Required Connection")]
        public long? RequiredConnectionId { get; set; }

        #region Relation Ships

        [RewriteableIgnore]
        [Browsable(false)]
        public virtual SSHConnectionInfo RequiredConnection { get { return _RequiredConnection; } set { SetProp(ref _RequiredConnection, (value == SSHConnectionInfo.Empty) ? null : value); } }
        [NonSerialized]
        private SSHConnectionInfo _RequiredConnection;

        #endregion

        #region Save as file

        /// <summary>
        /// Saves this connection info as a .rdp file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void SaveAs(string filepath, string host = null, int? port = null)
        {
            //winposstr:s:0,1,562,531,2482,1571
            File.WriteAllText(filepath,
                $@"
screen mode id:i:{(FullScreen ? 2 : 1)}
{(DesktopWidth.HasValue ? $"desktopwidth:i:{DesktopWidth.Value}" : "")}
{(DesktopHeight.HasValue ? $"desktopheight:i:{DesktopHeight.Value}" : "")}
username:s:{Username}
use multimon:i:0
session bpp:i:32
compression:i:1
keyboardhook:i:2
audiocapturemode:i:0
videoplaybackmode:i:1
connection type:i:7
networkautodetect:i:1
bandwidthautodetect:i:1
displayconnectionbar:i:1
enableworkspacereconnect:i:0
disable wallpaper:i:0
allow font smoothing:i:0
allow desktop composition:i:0
disable full window drag:i:1
disable menu anims:i:1
disable themes:i:0
disable cursor setting:i:0
bitmapcachepersistenable:i:1
full address:s:{host ?? Host}:{port ?? (Port > 0 ? Port : 3389)}
audiomode:i:1
redirectprinters:i:1
redirectcomports:i:0
redirectsmartcards:i:1
redirectclipboard:i:1
redirectposdevices:i:0
drivestoredirect:s:
autoreconnection enabled:i:1
authentication level:i:2
prompt for credentials:i:0
negotiate security layer:i:1
remoteapplicationmode:i:0
alternate shell:s:
shell working directory:s:
gatewayhostname:s:
gatewayusagemethod:i:4
gatewaycredentialssource:i:4
gatewayprofileusagemethod:i:0
promptcredentialonce:i:0
gatewaybrokeringtype:i:0
use redirection server name:i:0
rdgiskdcproxy:i:0
kdcproxyname:s:
");
        }

        #endregion

        #region AvailableRequiredConnections

        /// <summary>
        /// Enumerates connection infos that can be assigned to this <see cref="RequiredConnection"/>.
        /// </summary>
        [IgnoreDataMember]
        [RewriteableIgnore]
        [Browsable(false)]
        [NotMapped]
        public virtual IEnumerable<SSHConnectionInfo> AvailableRequiredConnections => SSHConnectionInfo.All;

        #endregion

        #region Desktop resolution suggestion

        [NotMapped]
        [IgnoreDataMember]
        [RewriteableIgnore]
        [Category("Screen"), PropertyOrder(2), DisplayName("Desktop Size")]
        public DesktopResulution DesktopResulution
        {
            get => DesktopResulution.Find(DesktopWidth ?? 0, DesktopHeight ?? 0);
            set
            {
                if (value != null)
                {
                    DesktopWidth = value.Width;
                    DesktopHeight = value.Height;
                }
            }
        }

        #endregion
    }
}
