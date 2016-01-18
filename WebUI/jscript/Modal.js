/*****************************************************************************\

 Javascript "Imagenius Web Service Client" library
 Modal management
 
 @version: 1.0 - 2010.05.24
 @author: Jonathan Betser
\*****************************************************************************/
var IGMODALTYPE_TEXTBOX = 0;
var IGMODALTYPE_COMBOBOX = 1;
var IGMODALTYPE_BUTTON = 2;
var IGMODALTYPE_COLOR = 3;
var IGMODALTYPE_TICKBOX = 4;
var IGMODALTYPE_MESSAGE = 5;
var IGMODALTYPE_IFRAME = 6;
var IGMODALTYPE_ERROR = 7;
var IGMODALTYPE_DIALOG = 8;

var IGMODALRETURN_CANCEL = 0;
var IGMODALRETURN_OK = 1;
var IGMODALRETURN_REPORTPB = 2;
var IGMODALRETURN_RECONNECT = 3;

function IG_Modal() {  
    this.square = null; 
    this.overdiv = null; 
    this.title = null; 
    this.callback = null; 
    this.tOptions = {};

    this.popOut = function (tParams) {
        var idxParam = 1;
        if (typeof tParams[0] === 'string')
            this.title = tParams[0];
        else {
            this.tOptions = tParams[0];
            this.title = tParams[1];
            idxParam = 2;
        }
        this.callback = tParams[idxParam++];
        this.successCallback = tParams[idxParam++];
        this.errorCallback = tParams[idxParam++];
        var nbParams = tParams[idxParam++];
        this.overdiv = document.createElement("div");
        this.overdiv.className = "overlay";
        this.overdiv.id = "IG_Modal";
        this.overdiv.style.visibility = "visible";
        this.overdiv.style.display = "inline-block";

        this.square = document.createElement("div");
        this.square.className = "popupNotHidden";
        this.square.style.visibility = "visible";
        this.square.style.display = "inline-block";
        this.square.Code = this;
        var msg = document.createElement("div");
        msg.className = "msg";
        msg.innerHTML = this.title;
        msg.setAttribute("style", "font-size: large");

        this.square.appendChild(msg);

        var tableParams = document.createElement("table");
        var curIdx = idxParam;
        this.dialogType = IGMODALTYPE_DIALOG;
        for (var idxParam = 0; idxParam < nbParams; idxParam++) {
            var nextField = null;
            var curType = tParams[curIdx++];
            switch (curType) {
                case IGMODALTYPE_TEXTBOX:
                    nextField = document.createElement("input");
                    nextField.id = "input" + idxParam.toString();
                    nextField.value = tParams[curIdx++];
                    this.tOptions[idxParam] = nextField.value;
                    nextField.onchange = function () {
                        var modal = this.parentNode.parentNode.parentNode.parentNode.Code;
                        modal.tOptions[Number(this.id.substring(5))] = this.value;
                    }
                    break;

                case IGMODALTYPE_COMBOBOX:
                    nextField = document.createElement("select");
                    nextField.id = "select" + idxParam.toString();
                    nextField.size = tParams[curIdx++];
                    nextField.onchange = function () {
                        var modal = this.parentNode.parentNode.parentNode.parentNode.Code;
                        var idxSel = (this.selectedIndex >= 0) ? this.selectedIndex : 0;
                        modal.tOptions[Number(this.id.substring(6))] = this.children[idxSel].innerHTML;
                    }
                    this.tOptions[idxParam] = tParams[curIdx];
                    for (var idxSel = 0; idxSel < nextField.size; idxSel++) {
                        var nextOption = document.createElement("option");
                        nextOption.value = idxSel;
                        nextOption.appendChild(document.createTextNode(tParams[curIdx++]));
                        nextField.appendChild(nextOption);
                    }
                    nextField.selectedIndex = 0;
                    break;

                case IGMODALTYPE_MESSAGE:
                case IGMODALTYPE_ERROR:
                    nextField = document.createElement("a");
                    nextField.setAttribute("style", "font-size: large");
                    var nextFieldText = document.createTextNode(tParams[curIdx++]);
                    nextField.appendChild(nextFieldText);
                    this.dialogType = curType;
                    break;

                case IGMODALTYPE_IFRAME:
                    dialogType = curType;
                    nextField = document.createElement("iframe");
                    var width = tParams[curIdx++];
                    var height = tParams[curIdx++];
                    this.square.setAttribute("style", "width: " + width + "px; height: " + height + "px");
                    nextField.setAttribute("style", "width: " + (width - 20) + "px; height: " + (height - 100) + "px");
                    nextField.setAttribute("src", tParams[curIdx++]);
                    this.dialogType = curType;
                    break;
            }
            var nextRow = document.createElement("tr");
            var nextCol = document.createElement("th");

            if (curType != IGMODALTYPE_IFRAME &&
                curType != IGMODALTYPE_MESSAGE &&
                curType != IGMODALTYPE_ERROR) {
                var nextLabel = document.createElement("a");
                var nextLabelText = document.createTextNode(tParams[curIdx++]);
                nextLabel.appendChild(nextLabelText);
                nextCol.appendChild(nextLabel);
                nextRow.appendChild(nextCol);
                nextCol = document.createElement("th");
            }

            nextCol.appendChild(nextField);
            nextRow.appendChild(nextCol);
            tableParams.appendChild(nextRow);
        }
        var critical = false;
        if (this.title == "Error" || this.title == "error") {
            this.dialogType = IGMODALTYPE_ERROR;
            critical = true;
        }

        var lastRow = document.createElement("tr");
        var lastRowCurCol = document.createElement("th");

        var okBtn = document.createElement("button");
        okBtn.onclick = function () {
            var modal = this.parentNode.parentNode.parentNode.parentNode.Code;
            modal.popIn();
            if (modal.callback)
                modal.callback(modal.dialogType == IGMODALTYPE_MESSAGE ? IGMODALRETURN_OK : IGMODALRETURN_RECONNECT, modal.tOptions, this.successCallback, this.errorCallback);
        }
        okBtn.innerHTML = (this.tOptions['OK'] != null ? this.tOptions['OK'] : (this.dialogType == IGMODALTYPE_IFRAME ? "Close" : (this.dialogType == IGMODALTYPE_ERROR ? "Reconnect" : "OK")));
        lastRowCurCol.appendChild(okBtn);

        if (critical) {
            var reportPbBtn = document.createElement("button");
            reportPbBtn.onclick = function () {
                var modal = this.parentNode.parentNode.parentNode.parentNode.Code;
                modal.popIn();
                if (modal.callback)
                    modal.callback(IGMODALRETURN_REPORTPB, modal.tOptions, this.successCallback, this.errorCallback);
            }
            reportPbBtn.innerHTML = "Report a problem";
            lastRowCurCol.appendChild(reportPbBtn);
        }

        if (this.dialogType != IGMODALTYPE_MESSAGE && this.dialogType != IGMODALTYPE_IFRAME) {
            var cancelBtn = document.createElement("button");
            cancelBtn.onclick = function () {
                var modal = this.parentNode.parentNode.parentNode.parentNode.Code;
                modal.popIn();
                modal.callback(IGMODALRETURN_CANCEL, null, this.successCallback, this.errorCallback);
            }
            cancelBtn.innerHTML = "Cancel";
            lastRowCurCol.appendChild(cancelBtn);
        }

        lastRow.appendChild(lastRowCurCol);
        tableParams.appendChild(lastRow);
        this.square.appendChild(tableParams);

        document.body.appendChild(this.overdiv);
        document.body.appendChild(this.square);
    } 
    
    this.popIn = function() { 
        if (this.square) { 
            document.body.removeChild(this.square); 
            this.square = null; 
        } 
        if (this.overdiv) { 
        document.body.removeChild(this.overdiv); 
        this.overdiv = null; 
        }   
    } 
} 

function IGOverLayer(divId, divClass, parentDiv) {  
    this.divId = divId;
    this.divClass = divClass;
    this.parentDiv = parentDiv;
    this.mouseoverCallback = null; 
    this.mousedownCallback = null; 
    this.mouseupCallback = null; 
    this.divOverlayer = null; 

    this.getDiv = function() {
        return this.divOverlayer;
    };

    this.getWindow = function() {
        var divParent = this.parentDiv;
        if (!divParent)
            return null;
        if (divParent.document)
            return divParent.document.parentWindow;   // IE8
        return divParent.ownerDocument.defaultView;   // Chrome
    };

    this.popOut = function (tParams) {
        this.divOverlayer = document.createElement("div");
        this.divOverlayer.setAttribute("class", this.divClass);
        this.divOverlayer.setAttribute("id", this.divId);
        var offsets = $('#' + this.parentDiv.getAttribute("id")).offset();
        this.divOverlayer.style.left = offsets.left.toString() + "px";
        this.divOverlayer.style.top = offsets.top.toString() + "px";
        var IG_deepZoomPanel = IGWS_DEEPZOOM_DIV;
        this.divOverlayer.style.width = IG_deepZoomPanel.style.width.toString() + "px";
        this.divOverlayer.style.height = IG_deepZoomPanel.style.height.toString() + "px";
        this.divOverlayer.Code = this;

        if (tParams) {   // register event handlers
            if (tParams[0]) {
                this.mousemoveCallback = tParams[0];
                this.divOverlayer.onmousemove = function (deepZoomEvent) {
                    if (!deepZoomEvent)
                        deepZoomEvent = this.Code.getWindow().event;
                    if (this.Code.mousemoveCallback && deepZoomEvent)
                        this.Code.mousemoveCallback(deepZoomEvent);
                };
            }
            if (tParams[1]) {
                this.mousedownCallback = tParams[1];
                this.divOverlayer.onmousedown = function (deepZoomEvent) {
                    if (!deepZoomEvent)
                        deepZoomEvent = this.Code.getWindow().event;
                    if (this.Code.mousedownCallback && deepZoomEvent)
                        this.Code.mousedownCallback(deepZoomEvent);
                };
            }
            if (tParams[2]) {
                this.mouseupCallback = tParams[2];
                this.divOverlayer.onmouseup = function (deepZoomEvent) {
                    if (!deepZoomEvent)
                        deepZoomEvent = this.Code.getWindow().event;
                    if (this.Code.mouseupCallback && deepZoomEvent)
                        this.Code.mouseupCallback(deepZoomEvent);
                };
            }
            if (tParams[3]) {
                this.mousescrollCallback = tParams[3];
                this.divOverlayer.onmousewheel = function (deepZoomEvent) {
                    if (!deepZoomEvent)
                        deepZoomEvent = this.Code.getWindow().event;
                    if (this.Code.mousescrollCallback && deepZoomEvent)
                        this.Code.mousescrollCallback(deepZoomEvent);
                };
            }
            this.selectCallback = tParams[4];
            if (this.selectCallback)
                this.selectCallback();
        }
        var divParent = this.parentDiv;
        if (divParent)
            divParent.appendChild(this.divOverlayer);
        if (this.getWindow())
            this.getWindow().overLayer = this;
        return this.divOverlayer;
    };

    this.popIn = function() { 
        if (this.divOverlayer) { 
            this.getWindow().overLayer = null;
            this.divOverlayer.parentNode.removeChild (this.divOverlayer);  
            this.divOverlayer = null;            
        }
    };
}

function IG_internalGetUpperLayer()
{
    var upperLayerDivId = "IG_divUpperlayer";
    var divOverlayer = window.document.getElementById(upperLayerDivId);
    if (divOverlayer)
        return divOverlayer.Code;
    return new IGOverLayer(upperLayerDivId, "upperlayer", IGWS_DEEPZOOM_DIV);
}

function IG_internalAlertClient(message, isError, title) {
    if (isError) {
        if (!title)
            title = "Error";
    }
    if (!message || (message.indexOf("Internal") == 0)) {
        alert(message);
        return; // avoid showing off ugly error messages
    }
    var IG_Modal_id = window.document.getElementById("IG_Modal");
    if (IG_Modal_id == null) {
        var modal = new IG_Modal();
        var idxParam = 0;
        var tParams = {};
        tParams[idxParam++] = (title != null ? title : (isError ? "Please reconnect" : "Information"));
        tParams[idxParam++] = function (result, tOptions) {
            if (result == IGMODALRETURN_REPORTPB)
                IG_internalContactUs("I have found a bug");
            else if (result == IGMODALRETURN_RECONNECT)
                location.reload(false);
        };
        tParams[idxParam++] = null;
        tParams[idxParam++] = null;
        tParams[idxParam++] = 1;
        tParams[idxParam++] = isError ? IGMODALTYPE_ERROR : IGMODALTYPE_MESSAGE;
        tParams[idxParam++] = message;
        modal.popOut(tParams);
    }
}