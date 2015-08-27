<!DOCTYPE html>
<html lang="en"><head><meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
    <meta charset="utf-8">
    <title>MiDAX</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <meta name="description" content="DAX Trading Signals"/>
    <meta name="author" content="Bitlsoft"/>
    <meta property="og:url" content="http://bitlsoft.com/midax/midax.aspx" />
    <meta property="og:title" content="MiDAX" />
    <meta property="og:description" content="DAX Trading Signals" />
    <meta property="og:image" content="http://bitlsoft.com/images/logo.png" />

    <!-- official bootstrap styles -->
    <link rel="stylesheet" href="//netdna.bootstrapcdn.com/bootstrap/3.1.1/css/bootstrap.min.css">
    <link rel="stylesheet" href="//netdna.bootstrapcdn.com/bootstrap/3.1.1/css/bootstrap-theme.min.css">

    <!-- Le styles -->
    <link rel="stylesheet" href="css/bootstrap/style.css"/>
    <link href="css/bootstrap/bootstrap.css" rel="stylesheet">
    
    <style type="text/css">
      body {
        padding-top: 20px;
        padding-bottom: 60px;
      }

      /* Custom container */
      .container {
        margin: 0 auto;
        max-width: 1000px;
      }
      .container > hr {
        margin: 60px 0;
      }

      /* Main marketing message and sign up button */
      .jumbotron {
        margin: 80px 0;
        text-align: center;
      }
      .jumbotron h1 {
        font-size: 100px;
        line-height: 1;
      }
      .jumbotron .lead {
        font-size: 24px;
        line-height: 1.25;
      }
      .jumbotron .btn {
        font-size: 21px;
        padding: 14px 24px;
      }

      /* Supporting marketing content */
      .marketing {
        margin: 60px 0;
      }
      .marketing p + h4 {
        margin-top: 28px;
      }


      /* Customize the navbar links to be fill the entire space of the .navbar */
      .navbar .navbar-inner {
        padding: 0;
      }
      .navbar .nav {
        margin: 0;
        display: table;
        width: 100%;
      }
      .navbar .nav li {
        display: table-cell;
        width: 1%;
        float: none;
      }
      .navbar .nav li a {
        font-weight: bold;
        text-align: center;
        border-left: 1px solid rgba(255,255,255,.75);
        border-right: 1px solid rgba(0,0,0,.1);
      }
      .navbar .nav li:first-child a {
        border-left: 0;
        border-radius: 3px 0 0 3px;
      }
      .navbar .nav li:last-child a {
        border-right: 0;
        border-radius: 0 3px 3px 0;
      }     

      body {
          font: 10px sans-serif;
        }

        .axis path,
        .axis line {
          fill: none;
          stroke: #000;
          shape-rendering: crispEdges;
        }

        .x.axis path {
          display: none;
        }

        .line {
          fill: none;
          stroke: steelblue;
          stroke-width: 1.5px;
        }
      
    </style>
    <link href="css/bootstrap/bootstrap-responsive.css" rel="stylesheet">

    <!-- HTML5 shim, for IE6-8 support of HTML5 elements -->
    <!--[if lt IE 9]>
      <script src="../assets/js/html5shiv.js"></script>
    <![endif]-->

    <!-- Fav and touch icons -->
    <link rel="apple-touch-icon-precomposed" sizes="144x144" href="ico/apple-touch-icon-144-precomposed.png">
    <link rel="apple-touch-icon-precomposed" sizes="114x114" href="ico/apple-touch-icon-114-precomposed.png">
    <link rel="apple-touch-icon-precomposed" sizes="72x72" href="ico/apple-touch-icon-72-precomposed.png">
    <link rel="apple-touch-icon-precomposed" href="ico/apple-touch-icon-57-precomposed.png">
    <link rel="shortcut icon" href="ico/favicon.png">
    <link rel="icon" type="image/png" href="images/favicon.ico" />

    <script src="jscript/jquery-1.9.1.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/d3/3.5.5/d3.min.js"></script>
	<script type="text/javascript" src="jscript/Init.js"></script>
    <script type="text/javascript" src="jscript/Modal.js"></script>
    <script type="text/javascript" src="jscript/Midax.js"></script>        
    <!--[if lte IE 9]>
    <script type='text/javascript' src='//cdnjs.cloudflare.com/ajax/libs/jquery-ajaxtransport-xdomainrequest/1.0.0/jquery.xdomainrequest.min.js'></script>
    <![endif]--> 
    <link rel="stylesheet" href="//code.jquery.com/ui/1.11.4/themes/smoothness/jquery-ui.css">
    <script src="//code.jquery.com/ui/1.11.4/jquery-ui.js"></script>
    <script>
      function formatDate(date) {
          return date.getFullYear() + "-" + (date.getMonth() + 1)  + "-" + date.getDate();
      }

      $(function () {
          $("#datepicker").datepicker();
      });
    </script>
    <script type="text/javascript">
        $(document).ready(function () { 
            $("#GO").click(function () {
                var currentDate = $("#datepicker").datepicker("getDate");
                if (currentDate == null)
                    currentDate = new Date();
                currentDate = formatDate(currentDate);
                var genericParams = { "begin": currentDate + " " + $('#timestart option:selected').text(), "end": currentDate + " " + $('#timestop option:selected').text() };
                var equityParams = $.extend({"stockid" : $("#equity").val()}, genericParams);
                
                if (window.document.getElementById("equity").selectedIndex > 0) {
                    var requests = { "GetStockData": equityParams };
                    if (window.document.getElementById("indicator").selectedIndex > 0) {
                        var indicatorIds = $("#indicator").val().split('#');
                        for(var id in indicatorIds){
                            var indicatorParams = $.extend({ "indicatorid": indicatorIds[id] + "_" + $("#equity").val() }, genericParams);
                            $.extend(requests, { "GetIndicatorData": indicatorParams });
                        }
                    }
                    if (window.document.getElementById("signal").selectedIndex > 0) {
                        var signalParams = $.extend({ "signalid": $("#signal").val() }, genericParams);
                        $.extend(requests, { "GetSignalData": signalParams });
                    }
                    MidaxAPI(requests);
                }
                else
                    IG_internalAlertClient("Please select at least one filter", false);
            });
        });
    </script>
  </head>

  <body>
    <div class="container">

      <div class="masthead">        
        <h3 class="muted">
        <div><table>
            <tr><th><img alt="BitL" src="images/logo_mini.png" class="logo"/></th>
                <th style="text-align: left; padding-left: 20px"><img alt="MiDAX" src="images/midax.png" class="logo" style="width: 150px"/><br/>DAX Trading Signals</th></tr></table></div></h3>
      </div>

      <!-- Jumbotron -->
      <div class="jumbotron" style="margin-top: 5px">   
        <div class="control-group">             
           <div class="controls">
             <input class="form-control input-medium" placeholder="Today" id="datepicker">
             <select class="combobox input-medium" id="timestart">
               <option value="0">06:45</option>
               <option value="15">07:00</option>
               <option value="45">07:30</option>
               <option value="75">08:00</option>
               <option value="135">09:00</option>
               <option value="195">10:00</option>
               <option value="255">11:00</option>
               <option value="315">12:00</option>
               <option value="375">13:00</option>
               <option value="435">14:00</option>
               <option value="495">15:00</option>
               <option value="555">16:00</option>
               <option value="615">17:00</option>
             </select>
             <select class="combobox input-medium" id="timestop">
               <!--option value="790">23:45</option!-->
               <option value="690">18:15</option>
               <option value="675">18:00</option>
               <option value="645">17:30</option>
               <option value="615">17:00</option>
               <option value="555">16:00</option>
               <option value="495">15:00</option>
               <option value="435">14:00</option>
               <option value="375">13:00</option>
               <option value="315">12:00</option>
               <option value="255">11:00</option>
               <option value="195">10:00</option>
               <option value="135">09:00</option>
               <option value="75">08:00</option>
               <option value="45">07:30</option>
             </select>
               <br />
             <select class="combobox input-large" id="equity">
               <option value="">Choose a market data</option>
               <option value="IX.D.DAX.DAILY.IP">DAX</option>
               <option value="ED.D.ADSGY.DAILY.IP">Adidas AG</option>
               <option value="ED.D.ALVGY.DAILY.IP">Allianz SE</option>
               <option value="ED.D.BAS.DAILY.IP">BASF SE</option>
               <option value="ED.D.BAY.DAILY.IP">Bayer AG</option>
               <option value="ED.D.BMW.DAILY.IP">Bayerische Motoren Werke AG</option>
               <option value="ED.D.BEI.DAILY.IP">Beiersdorf AG</option>
               <option value="ED.D.CBK.DAILY.IP">Commerzbank AG</option>
               <option value="ED.D.CON.DAILY.IP">Continental AG</option>
               <option value="ED.D.DCX.DAILY.IP">Daimler AG</option>
               <option value="ED.D.DBK.DAILY.IP">Deutsche Bank AG</option>
               <option value="ED.D.DB1.DAILY.IP">Deutsche Boerse AG</option>
               <option value="ED.D.LHAG.DAILY.IP">Deutsche Lufthansa AG</option>
               <option value="ED.D.DPW.DAILY.IP">Deutsche Post AG</option>
               <option value="ED.D.DTE.DAILY.IP">Deutsche Telekom AG</option>
               <option value="ED.D.EOA.DAILY.IP">E.ON SE</option>
               <option value="ED.D.FME.DAILY.IP">Fresenius Medical Care AG</option>
               <option value="ED.D.HEI.DAILY.IP">HeidelbergCement AG</option>
               <option value="ED.D.HENGY.DAILY.IP">Henkel AG</option>
               <option value="ED.D.IFX.DAILY.IP">Infineon Technologies AG</option>
               <option value="ED.D.SDF.DAILY.IP">K+S AG</option>
               <option value="ED.D.LXS.DAILY.IP">LANXESS AG</option>
               <option value="ED.D.LING.DAILY.IP">Linde AG</option>
               <option value="ED.D.MRCG.DAILY.IP">Merck KGaA</option>
               <option value="ED.D.MUV2.DAILY.IP">Muenchener Rueckversicherungs AG</option>
               <option value="ED.D.RWEG.DAILY.IP">RWE AG</option>
               <option value="ED.D.SAPG.DAILY.IP">SAP AG</option>
               <option value="ED.D.SIEGn.DAILY.IP">Siemens AG</option>
               <option value="ED.D.TKAG.DAILY.IP">ThyssenKrupp AG</option>
               <option value="ED.D.VOW.DAILY.IP">Volkswagen AG</option>
             </select>
             <select class="combobox input-large" id="indicator">
               <option value="">Choose an indicator</option>
               <option value="WMA_Low_5#WMA_High_60">WMA 5mn/1h</option>
             </select>   
             <select class="combobox input-large" id="signal">
               <option value="">Choose a signal</option>
               <option value="MacD">MacD</option>
             </select>          
             <button type="button" id="GO" class="btn btn-primary">
                    <span class="glyphicon glyphicon-ok"></span> </button>
            <div>
                <button type="button" id="Refresh" class="btn btn-danger btn-sm" onclick="document.location.reload();" style="float:right">
                    <span class="glyphicon glyphicon-refresh"></span>Clear!</button>
            </div>
           </div>
        </div>  
      </div>

      <div id="graphs">
      </div>

      <hr/>

      <div class="footer">
        <p>© B.I.T.L. 2015</p>
      </div>

    </div> <!-- /container -->

    <!-- Le javascript
    ================================================== -->
    <!-- Placed at the end of the document so the pages load faster -->
    <script src="./Template · Bootstrap_files/jquery.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-transition.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-alert.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-modal.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-dropdown.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-scrollspy.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-tab.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-tooltip.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-popover.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-button.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-collapse.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-carousel.js"></script>
    <script src="./Template · Bootstrap_files/bootstrap-typeahead.js"></script>
      
</body></html>