var ASSUMPTION_TREND = "";
var ALWAYS_TRADING = false;
var REMOVE_DUPLICATES = false;
var IS_LIVE = false;

var monthNames = [
  "Jan", "Feb", "Mar",
  "Apr", "May", "Jun", "Jul",
  "Augt", "Sep", "Oct",
  "Nov", "Dec"
];

function noStockWithIndicator(ind) {
    return (ind.indexOf("Volume_") != -1 || ind.indexOf("RSI_") != -1 || ind.indexOf("EvtProx_") != -1 ||
            ind.indexOf("Cor_") != -1 || ind.indexOf("Trend_") != -1 || ind.indexOf("WMVol_") != -1);
}

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

var graphCount = 0;

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
            if (d.n.startsWith("Low") || d.n.startsWith("High") || d.n.startsWith("Close"))
                d.t.setDate(d.t.getDate() + 1);
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
                if (sellValue == 0)
                    sellValue = buyValue + 80;
                if (buyValue == 0)
                    buyValue = sellValue - 80;
                if (ASSUMPTION_TREND == "BEAR") {
                    var buyFirst = false;
                    do {
                        buyFirst = jsonData[marketData].response[jsonData[marketData].response.length - 1].b == buyValue;
                        if (buyFirst)
                            jsonData[marketData].response.splice(jsonData[marketData].response.length - 1, 1);
                        if (jsonData[marketData].response.length == 0)
                            buyFirst = false;
                    } while (buyFirst);
                }
                else if (ASSUMPTION_TREND == "BULL") {
                    var sellFirst = false;
                    do {
                        sellFirst = jsonData[marketData].response[jsonData[marketData].response.length - 1].b == sellValue;
                        if (sellFirst)
                            jsonData[marketData].response.splice(jsonData[marketData].response.length - 1, 1);
                        if (jsonData[marketData].response.length == 0)
                            sellFirst = false;
                    } while (sellFirst);
                }
                var nbBuy = 0;
                var nbSell = 0;
                jsonData[marketData].response.forEach(function (d) {
                    if (d.b == buyValue)
                        nbBuy++;
                    else
                        nbSell++;
                });
                var quoteKey = "GetStockData0";
                var lastQuoteTime = new Date(jsonData[quoteKey].response[0].t);
                if ((nbBuy > 0 || nbSell > 0) && IS_LIVE) {
                    var val = jsonData[marketData].response[0].b == buyValue ? sellValue : buyValue;
                    jsonData[marketData].response.unshift({
                        t: jsonData[quoteKey].response[0].t,
                        b: val,
                        o: jsonData[quoteKey].response[0].o,
                        v: jsonData[quoteKey].response[0].v,
                        n: jsonData[marketData].response[0].n,
                        s: jsonData[quoteKey].response[0].s
                    });
                }
                else if (nbBuy != nbSell) {
                    if (nbBuy > nbSell) {
                        var buyLast = false;
                        do {
                            buyLast = jsonData[marketData].response[0].b == buyValue;
                            if (buyLast) {
                                jsonData[marketData].response.splice(0, 1);
                                nbBuy--;
                            }
                            if (jsonData[marketData].response.length == 0)
                                buyLast = false;
                        } while (buyLast && nbBuy != nbSell);
                    }
                    else {
                        var sellLast = false;
                        do {
                            sellLast = jsonData[marketData].response[0].b == sellValue;
                            if (sellLast) {
                                jsonData[marketData].response.splice(0, 1);
                                nbSell--;
                            }
                            if (jsonData[marketData].response.length == 0)
                                sellLast = false;
                        } while (sellLast && nbBuy != nbSell);
                    }
                }
                if (REMOVE_DUPLICATES) {
                    var signals = [];
                    var idxSignal = 0;
                    var lastSignal = null;
                    nbBuy = 0;
                    nbSell = 0;
                    jsonData[marketData].response.slice().reverse().forEach(function (d) {
                        var skip = false;
                        if (idxSignal > 0 && d.b == lastSignal.b && REMOVE_DUPLICATES)
                            skip = true;
                        if (!skip) {
                            signals.unshift(d);
                            if (d.b == buyValue)
                                nbBuy++;
                            else
                                nbSell++;
                        }
                        lastSignal = d;
                        idxSignal++;
                    });
                    jsonData[marketData].response = signals;
                }
                if (nbBuy == nbSell || ASSUMPTION_TREND == "") {
                    var signalProfit = 0;
                    var idxSignal = 0;
                    jsonData[marketData].response.forEach(function (d) {
                        var coeff = ((idxSignal > 0) && (idxSignal < nbBuy + nbSell - 1) && ASSUMPTION_TREND == "" && ALWAYS_TRADING) ? 2.0 : 1.0;
                        signalProfit += d.o * coeff * (d.b == buyValue ? -1 : 1);
                        idxSignal++;
                    });
                    profit += signalProfit;
                    jsonData[marketData].profit = profit;
                }
                else  // Set an error value if the orders do not match
                    profit = 1000000;
            }
        }
        jsonData[marketData].response.forEach(function (d) {
            if (!keyValueMap[d.n])
                keyValueMap[d.n] = [];
            keyValueMap[d.n].push({
                t: d.t,
                b: d.b,
                o: d.o,
                v: d.v
            });
        });
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
    var perfSuffix = (profit != null ? " Performance: " + (Math.abs(profit) > 1500 ? "?" : profit) : "");
    svg.append("text")
        .attr("x", (width / 2))
        .attr("y", 5 - (margin.top / 2))
        .attr("text-anchor", "middle")
        .style("font-size", "16px")
        .text(keyValueMap[firstStock][0].t.getDate() + " " + monthNames[keyValueMap[firstStock][0].t.getMonth()] + " " + keyValueMap[firstStock][0].t.getFullYear() + perfSuffix);

    var graphCountStr = graphCount.toString();
    svg.append("path") // this is the black vertical line to follow mouse
      .attr("class", "mouseLine")
      .attr("id", "mouseLine" + graphCountStr)
      .style("stroke", "black")
      .style("stroke-width", "1px")
      .style("opacity", "0");

    var mouseCircle = quote.append("g") // for each line, add group to hold text and circle
          .attr("class", "mouseCircle")
          .attr("id", "mouseCircle" + graphCountStr);

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
          d3.select("#mouseLine" + graphCountStr)
              .style("opacity", "0");
          d3.selectAll("#mouseCircle" + graphCountStr)
              .style("opacity", "0");
      })
      .on('mouseover', function () { // on mouse in show line, circles and text
          d3.select("#mouseLine" + graphCountStr)
              .style("opacity", "1");
          d3.selectAll("#mouseCircle" + graphCountStr)
             .style("opacity", "1");
      })
      .on('mousemove', function () { // mouse moving over canvas
          d3.select("#mouseLine" + graphCountStr)
          .attr("d", function () {
              yRange = y.range(); // range of y axis
              var xCoor = d3.mouse(this)[0]; // mouse position in x
              var xDate = x.invert(xCoor); // date corresponding to mouse x 
              d3.selectAll("#mouseCircle" + graphCountStr) // for each circle group
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

    graphCount = graphCount + 1;
    return profit;
}

function calcStdDev(profits, avgProfit) {
    var variance = 0.0;
    for (var idxProfit = 0; idxProfit < profits.length; idxProfit++) {
        variance += Math.pow(profits[idxProfit] - avgProfit, 2.0);
    }
    return Math.sqrt(variance / profits.length);
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
                if (!("profitList" in sync))
                    sync['profitList'] = new Array(sync['nbDays']);
                sync['profitList'][sync['processedDays'] - 1] = profit;
                if (sync['processedDays'] == sync['nbDays'] && sync['nbDays'] > 1) {
                    var leverage = 5.0 / 1000.0; // assuming 0.5% leverage
                    var returnRates = new Array(sync['nbDays']);
                    var avgReturnRate = 0.0;
                    for (var idxProfit = 0; idxProfit < returnRates.length; idxProfit++) {
                        returnRates[idxProfit] = sync['profitList'][idxProfit] * 30.5 * 12.0 * leverage;
                        avgReturnRate += returnRates[idxProfit];
                    }
                    avgReturnRate /= returnRates.length;
                    var stdDev = calcStdDev(returnRates, avgReturnRate);
                    var sharpe = (avgReturnRate - 0.5) / stdDev; // assuming an average 0.5 treasury rate
                    IG_internalAlertClient("Total profits(bps): " + sync['profits'] + ", return rate: " + Math.round(avgReturnRate * 100) / 100.0
                        + ", std dev: " + Math.round(stdDev * 100) / 100.0 + ", sharpe ratio: " + Math.round(sharpe * 100) / 100.0, false, "Model results");
                }

            }            
        }
    });
}

function MidaxAPI(requests, sync) {
    recursiveAPICalls(requests, 0, sync);
}