to convert historical data files from histdata.com to Midax format:
- open the file in Notepad++
- add a blank line at the top
- apply the following Replace regexp (for EURUSD May 2017):
Find what: ([0-9]{4})+([0-9]{2})+([0-9]{2}) ([0-9]{2})+([0-9]{2})+([0-9]{2})+([0-9]{3})
Replace with: stocks,CS.D.EURUSD.TODAY.IP,\3/\2/\1 \4:\5:\6.\7,EURUSD
- save as mktdata_eurusd_5_2017.csv in DBImporter\MktData