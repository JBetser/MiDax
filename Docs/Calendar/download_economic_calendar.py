import requests, xlrd, datetime, csv

def append_row(row_list, sheet, row_index, new_date):
    new_row = [new_date]
    for col_index in xrange(1, sheet.ncols):
        new_row.append(sheet.cell(row_index, col_index).value)
    row_list.append(new_row)

last_sunday = datetime.date.today() - datetime.timedelta(datetime.date.today().weekday() + 1)
date_str = last_sunday.strftime("%m-%d-%Y")
url_str = 'http://www.dailyfx.com/files/Calendar-' + date_str + '.xls'
response = requests.get(url_str, stream=True,
                        headers={'User-agent': 'Mozilla/5.0'})
if response.status_code != 200:
    raise RuntimeError("Could not download the file: " + url_str)
with open('Calendar-' + date_str + '.csv', "wb") as f:
    data = response.raw.read()
    book = xlrd.open_workbook(file_contents=data, formatting_info=True)
    sheet = book.sheet_by_index(0)
    keys = [sheet.cell(0, col_index).value for col_index in xrange(sheet.ncols)]
    row_list = []
    has_started = False
    new_date = None
    for row_index in xrange(0, sheet.nrows):
        if sheet.cell(row_index, 0).value == 'Date':
            has_started = True
            append_row(row_list, sheet, row_index, 'Date')
        elif has_started:
            cur_date = sheet.cell(row_index, 0).value if sheet.cell(row_index, 0).value != '' else None
            if cur_date is not None:
                new_date = cur_date
            if book.xf_list[sheet.cell(row_index, 2).xf_index].font_index > 5:
                append_row(row_list, sheet, row_index, new_date)
    writer = csv.writer(f)
    writer.writerows(row_list)
