var monthNames = [
  "Jan", "Feb", "Mar",
  "Apr", "May", "Jun", "Jul",
  "Augt", "Sep", "Oct",
  "Nov", "Dec"
];

// from here: http://stackoverflow.com/a/1968345/16363
function get_line_intersection(p0_x, p0_y, p1_x, p1_y,
    p2_x, p2_y, p3_x, p3_y) {
    var rV = {};
    var s1_x, s1_y, s2_x, s2_y;
    s1_x = p1_x - p0_x; s1_y = p1_y - p0_y;
    s2_x = p3_x - p2_x; s2_y = p3_y - p2_y;

    var s, t;
    s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / (-s2_x * s1_y + s1_x * s2_y);
    t = (s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

    if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {
        // Collision detected
        rV.x = p0_x + (t * s1_x);
        rV.y = p0_y + (t * s1_y);
    }

    return rV;
}

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
    var profit = null;
    for (var marketData in jsonData) {
        jsonData[marketData].response.forEach(function (d) {
            d.t = new Date(d.t);
            if (!keyValueMap[d.n])
                keyValueMap[d.n] = [];
            if (d.n.startsWith("Low") || d.n.startsWith("High") || d.n.startsWith("Close"))
                d.t.setDate(d.t.getDate() + 1);
            keyValueMap[d.n].push({
                t: d.t,
                b: d.b,
                o: d.o,
                v: d.v
            });
        });
        if (marketData.lastIndexOf("GetSignalData", 0) === 0) {
            if (profit == null)
                profit = 0;
            if (jsonData[marketData].response.length > 0) {
                var buyValue = 0;
                var sellValue = 0;
                jsonData[marketData].response.forEach(function (d) {
                    if (buyValue == 0)
                        buyValue = d.b;
                    else if (buyValue > d.b) {
                        sellValue = buyValue;
                        buyValue = d.b;
                    }
                    else if (buyValue < d.b) {
                        sellValue = d.b;
                    }
                });
                var buyFirst = false;
                do {
                    buyFirst = jsonData[marketData].response[jsonData[marketData].response.length - 1].b == buyValue;
                    if (buyFirst)
                        jsonData[marketData].response.splice(jsonData[marketData].response.length - 1, 1);
                    if (jsonData[marketData].response.length == 0)
                        buyFirst = false;
                } while (buyFirst);
                var nbBuy = 0;
                var nbSell = 0;
                var signalProfit = 0;
                jsonData[marketData].response.forEach(function (d) {
                    signalProfit += d.o * (d.b == buyValue ? -1 : 1);
                    if (d.b == buyValue)
                        nbBuy++;
                    else
                        nbSell++;
                });
                if (nbBuy != nbSell) {
                    if (nbBuy > nbSell) {
                        var buyLast = false;
                        do {
                            buyLast = jsonData[marketData].response[0].b == buyValue;
                            if (buyLast)
                                jsonData[marketData].response.splice(0, 1);
                            if (jsonData[marketData].response.length == 0)
                                buyLast = false;
                        } while (buyLast);
                    }
                    else {
                        var sellLast = false;
                        do {
                            sellLast = jsonData[marketData].response[0].b == sellValue;
                            if (sellLast)
                                jsonData[marketData].response.splice(0, 1);
                            if (jsonData[marketData].response.length == 0)
                                sellLast = false;
                        } while (sellLast);
                    }
                    signalProfit = 0;
                    jsonData[marketData].response.forEach(function (d) {
                        signalProfit += d.o * (d.b == buyValue ? -1 : 1);
                        if (d.b == buyValue)
                            nbBuy++;
                        else
                            nbSell++;
                    });
                }
                profit += signalProfit;
            }
        }
    }

    var line = d3.svg.line()
        .interpolate("linear")
        .x(function (d) { return x(d.t); })
        .y(function (d) { return y(d.b); });

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
      d3.min(quotes, function (c) { return d3.min(c.values, function (v) { return v.b; }); }),
      d3.max(quotes, function (c) { return d3.max(c.values, function (v) { return v.b; }); })
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
        .attr("transform", function (d) { return "translate(" + x(d.value.t) + "," + y(d.value.b) + ")"; })
        .attr("x", 3)
        .attr("dy", ".35em")
        .text(function (d) { return d.n; });

    legend = svg.append("g")
        .attr("class", "legend")
        .attr("transform", "translate(50,30)")
        .style("font-size", "12px")
        .call(d3.legend)

    if (profit != null)
        profit = Math.round(profit * 100) / 100;
    var perfSuffix = (profit != null ? " Performance: " + (Math.abs(profit) > 1000 ? "?" : profit) : "");
    svg.append("text")
        .attr("x", (width / 2))
        .attr("y", 5 - (margin.top / 2))
        .attr("text-anchor", "middle")
        .style("font-size", "16px")
        .text(keyValueMap[firstStock][0].t.getDate() + " " + monthNames[keyValueMap[firstStock][0].t.getMonth()] + " " + keyValueMap[firstStock][0].t.getFullYear() + perfSuffix);

    svg.append("path") // this is the black vertical line to follow mouse
      .attr("class", "mouseLine")
      .style("stroke", "black")
      .style("stroke-width", "1px")
      .style("opacity", "0");

    var mouseCircle = quote.append("g") // for each line, add group to hold text and circle
          .attr("class", "mouseCircle");

    mouseCircle.append("circle") // add a circle to follow along path
      .attr("r", 7)
      .style("stroke", function (d) { console.log(d); return color(d.name); })
      .style("fill", "none")
      .style("stroke-width", "1px");

    mouseCircle.append("text")
      .attr("transform", "translate(10,3)"); // text to hold coordinates

    var bisect = d3.bisector(function (d) { return -d.t; }); // reusable bisect to find points before/after line

    svg.append('svg:rect') // append a rect to catch mouse movements on canvas
      .attr('width', width) // can't catch mouse events on a g element
      .attr('height', height)
      .attr('fill', 'none')
      .attr('pointer-events', 'all')
      .on('mouseout', function () { // on mouse out hide line, circles and text
          d3.select(".mouseLine")
              .style("opacity", "0");
          d3.selectAll(".mouseCircle circle")
              .style("opacity", "0");
          d3.selectAll(".mouseCircle text")
                .style("opacity", "0");
      })
      .on('mouseover', function () { // on mouse in show line, circles and text
          d3.select(".mouseLine")
              .style("opacity", "1");
          d3.selectAll(".mouseCircle circle")
             .style("opacity", "1");
          d3.selectAll(".mouseCircle text")
              .style("opacity", "1");
      })
      .on('mousemove', function () { // mouse moving over canvas
          d3.select(".mouseLine")
          .attr("d", function () {
              yRange = y.range(); // range of y axis
              var xCoor = d3.mouse(this)[0]; // mouse position in x
              var xDate = x.invert(xCoor); // date corresponding to mouse x 
              d3.selectAll('.mouseCircle') // for each circle group
                  .each(function (d, i) {
                      var rightIdx = bisect.left(quotes[i].values, -xDate); // find date in data that right off mouse
                      var interSect = get_line_intersection(xCoor,  // get the intersection of our vertical line and the data line
                           yRange[0],
                           xCoor,
                           yRange[1],
                           x(quotes[i].values[rightIdx - 1].t),
                           y(quotes[i].values[rightIdx - 1].b),
                           x(quotes[i].values[rightIdx].t),
                           y(quotes[i].values[rightIdx].b));

                      d3.select(this) // move the circle to intersection
                          .attr('transform', 'translate(' + interSect.x + ',' + interSect.y + ')');

                      d3.select(this.children[1]) // write coordinates out
                          .text(xDate.getHours() + ":" + xDate.getMinutes() + ":" + xDate.getSeconds() + " " + y.invert(interSect.y).toFixed(0));

                  });

              return "M" + xCoor + "," + yRange[0] + "L" + xCoor + "," + yRange[1]; // position vertical line
          });
      });

    return profit;
}

function internalAPI(functionName, jsonData, sync, successCallback, errorCallback) {
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
        async: sync['nbDays'] == 1,
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

function recursiveAPICalls(requests, idx, sync) {
    var key = Object.keys(requests)[idx];
    internalAPI(key.substring(0, key.length - 1), requests[key], sync, function (jsonResponse) {
        $.extend(requests[key], { "response": jsonResponse });
        if (idx < Object.keys(requests).length - 1)
            recursiveAPICalls(requests, idx + 1, sync);
        else {
            var profit = processResponses(requests);
            if (profit != null) {
                if (sync['profits'] == null)
                    sync['profits'] = 0;
                if (sync['profits'] != "?")
                    sync['profits'] += profit;
                if (Math.abs(profit) > 1000)
                    sync['profits'] = "?";
                sync['processedDays'] += 1;
                if (sync['processedDays'] == sync['nbDays'] && sync['nbDays'] > 1)
                    IG_internalAlertClient("Total profits: " + sync['profits'], false, "Model results");
            }
        }
    });
}

function MidaxAPI(requests, sync) {
    recursiveAPICalls(requests, 0, sync);
}