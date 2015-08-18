
function ProcessAnswersCallback(jsonData, successCallback, errorCallback) {
    var margin = { top: 20, right: 80, bottom: 30, left: 50 },
        width = 960 - margin.left - margin.right,
        height = 500 - margin.top - margin.bottom;

    var parseDate = d3.time.format("%Y-%m-%d %H:%M:%S").parse;

    var x = d3.time.scale()
        .range([0, width]);

    var y = d3.scale.linear()
        .range([height, 0]);

    var color = d3.scale.category10();

    var xAxis = d3.svg.axis()
        .scale(x)
        .orient("bottom");

    var yAxis = d3.svg.axis()
        .scale(y)
        .orient("left");

    var line = d3.svg.line()
        .interpolate("basis")
        .x(function (d) { return x(d.trading_time); })
        .y(function (d) { return y((d.bid + d.offer)/2); });

    var svg = d3.select("body").append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom)
      .append("g")
        .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

    var keyValueMap = {};
    jsonData.forEach(function (d) {
        d.trading_time = new Date(d.trading_time);
        if (!keyValueMap[d.name])
            keyValueMap[d.name] = []
        keyValueMap[d.name].push({
            trading_time: d.trading_time,
            bid: d.bid,
            offer: d.offer,
            mid: (d.bid + d.offer) / 2,
            volume: d.volume
        });
    });
    
    color.domain(Object.keys(keyValueMap));
    
    jsonData.forEach(function (d) {
        d.trading_time = new Date(d.trading_time);
    });

    var quotes = color.domain().map(function (stock_name) {
        return {
            name: stock_name,
            values: keyValueMap[stock_name]
        };
    });

    x.domain(d3.extent(jsonData, function (d) { return d.trading_time; }));

    y.domain([
      d3.min(quotes, function (c) { return d3.min(c.values, function (v) { return v.mid; }); }),
      d3.max(quotes, function (c) { return d3.max(c.values, function (v) { return v.mid; }); })
    ]);

    svg.append("g")
        .attr("class", "x axis")
        .attr("transform", "translate(0," + height + ")")
        .call(xAxis);

    svg.append("g")
        .attr("class", "y axis")
        .call(yAxis)
      .append("text")
        .attr("transform", "rotate(-90)")
        .attr("y", 6)
        .attr("dy", ".71em")
        .style("text-anchor", "end")
        .text("Quote (Mid)");

    var quote = svg.selectAll(".quote")
        .data(quotes)
      .enter().append("g")
        .attr("class", "quote");

    quote.append("path")
        .attr("class", "line")
        .attr("d", function (d) { return line(d.values); })
        .style("stroke", function (d) { return color(d.name); });

    quote.append("text")
        .datum(function (d) { return { name: d.name, value: d.values[d.values.length - 1].mid }; })
        .attr("transform", function (d) { return "translate(" + x(d.value.trading_time) + "," + y(d.value.mid) + ")"; })
        .attr("x", 3)
        .attr("dy", ".35em")
        .text(function (d) { return d.name; });
}


function MidaxAPI(functionName, jsonData, successCallback, errorCallback) {
    if (jsonData == null)
        jsonData = {};
    var asmxName = null;
    switch (functionName) {
        case "GetStockData":
        case "GetIndicatorData":
        case "GetSignalData":
            asmxName = "midax.asmx/";
            break;
        default:
            if (errorCallback)
                errorCallback("Error", "Unknown function name");
            return;
    }
    $.ajax({
        url: IG_WEBSERVICE_URL + asmxName + functionName,
        data: JSON.stringify(jsonData),
        dataType: 'json',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        crossDomain: true,
        success: function (data) {
            var jsonData = jQuery.parseJSON(data.d);
            ProcessAnswersCallback(jsonData, successCallback, errorCallback);
        },
        error: function (data, status, jqXHR) {
            if (errorCallback)
                errorCallback(status, jqXHR);
            else {
                IG_internalAlertClient("Could not retrieve data from server", true);
            }
        }
    });
}