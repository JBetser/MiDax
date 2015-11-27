
function processResponses(jsonData) {
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
    
    var svg = d3.select("#graphs").append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom)
      .append("g")
        .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

    var keyValueMap = {};
    for (var marketData in jsonData) {
        jsonData[marketData].response.forEach(function (d) {
            d.t = new Date(d.t);
            if (!keyValueMap[d.n])
                keyValueMap[d.n] = []
            keyValueMap[d.n].push({
                t: d.t,
                b: d.b,
                o: d.o,
                m: (d.b + d.o) / 2,
                v: d.v
            });
        });
    }

    var line = d3.svg.line()
        .interpolate("linear")
        .x(function (d) { return x(d.t); })
        .y(function (d) { return y((d.b + d.o) / 2); });
    
    var stocks = Object.keys(keyValueMap);
    color.domain(stocks);

    var quotes = color.domain().map(function (stock_name) {
        return {
            name: stock_name,
            values: keyValueMap[stock_name]
        };
    });

    var firstStock = stocks[0];
    x.domain(d3.extent(keyValueMap[firstStock], function (d) { return d.t; }));

    y.domain([
      d3.min(quotes, function (c) { return d3.min(c.values, function (v) { return v.m; }); }),
      d3.max(quotes, function (c) { return d3.max(c.values, function (v) { return v.m; }); })
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
        .text("Mid");

    var quote = svg.selectAll(".quote")
        .data(quotes)
      .enter().append("g")
        .attr("class", "quote");

    quote.append("path")
        .attr("class", "line")
        .attr("d", function (d) { return line(d.values); })
        .attr("data-legend", function (d) { return d.name })
        .style("stroke", function (d) { return color(d.name); });

    quote.append("text")
        .datum(function (d) { return { name: d.name, value: d.values[d.values.length - 1] }; })
        .attr("transform", function (d) { return "translate(" + x(d.value.t) + "," + y(d.value.m) + ")"; })
        .attr("x", 3)
        .attr("dy", ".35em")
        .text(function (d) { return d.n; });

    legend = svg.append("g")
        .attr("class", "legend")
        .attr("transform", "translate(50,30)")
        .style("font-size", "12px")
        .call(d3.legend)
}

function internalAPI(functionName, jsonData, successCallback, errorCallback) {
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
            if (successCallback)
                successCallback(jQuery.parseJSON(data.d));
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

function recursiveAPICalls(requests, idx)
{
    var key = Object.keys(requests)[idx];
    internalAPI(key.substring(0, key.length - 1), requests[key], function (jsonResponse) {
        $.extend(requests[key], { "response": jsonResponse });
        if (idx < Object.keys(requests).length - 1)
            recursiveAPICalls(requests, idx + 1);
        else
            processResponses(requests)
    });
}

function MidaxAPI(requests) {
    recursiveAPICalls(requests, 0);
}