import datetime, os, sys
from subprocess import Popen, PIPE

date_str = datetime.date.today().strftime("%Y-%m-%d")
print('Publishing levels for date: ' + date_str)
date_arg = '-DATE' + date_str
midax_tester_path = os.path.join('D:\\', 'Shared', 'MidaxTester', 'MidaxTester.exe')
process = Popen([midax_tester_path, '-G', '-Heuristic', date_arg, '-FROMDB', '-TODB', '-FULL', '-TOPROD'], stdout=PIPE, stderr=PIPE)
stdout, stderr = process.communicate()
print('Publishing levels for date: ' + date_str + ' completed')