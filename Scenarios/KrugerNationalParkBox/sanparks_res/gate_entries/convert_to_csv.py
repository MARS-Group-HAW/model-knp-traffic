import pandas as pd

# prevent false warning
# https://stackoverflow.com/questions/20625582/how-to-deal-with-settingwithcopywarning-in-pandas
pd.options.mode.chained_assignment = None  # default='warn'

import matplotlib.pyplot as plt
import matplotlib.ticker as mtick
import numpy as np
import openpyxl # needed for pd to read excel

import math
import re
import datetime
import glob 


def get_start_date(text):
    m = re.search("From: (\d{4}-\d{2}-\d{2})", text)
    
    if not m:
        print("No Start date found")
        return None
    
    return datetime.date(*map(int, m[1].split('-')))

def get_end_date(text):
    m = re.search("To: (\d{4}-\d{2}-\d{2})", text)
    
    if not m:
        print("No Start date found")
        return None
    
    return datetime.date(*map(int, m[1].split('-')))

def get_start_time(s):
    m = s.split('-')
    return m[0].strip()
def get_end_time(s):
    m = s.split('-')
    return m[1].strip()


files = glob.glob("*.xlsx")

for file_name in files:

    # skip tmp files if excel file is opend by Excel
    if file_name[0] == '~':
        continue

    print("Parsing {}...".format(file_name))

    # read in 
    df = pd.read_excel (file_name, header=None, usecols=[0, 1, 2, 3, 4], 
                    names=['hour', 'date', 'total', 'day', 'overnight'],
                   skiprows=3)

    # extract date range
    date_range = df.at[0, 'date']
    start_date = get_start_date(date_range)
    end_date   = get_end_date(date_range)
    df['start_date'] = start_date
    df['end_date']   = end_date

    # drop empty start rows
    df = df.iloc[6:]
    df = df.reset_index(drop=True)

    # sanity checks for sums + drop meta rows with totals
    rows_to_remove = [] # inidizes of rows containing cmap names
    current_camp = None

    df['camp'] = None
    for i, row in df.iterrows():

        # check if totals are correct
        if row['hour'] == 'TOTAL':
            dfx = df[df['camp'] == current_camp]
    
            total = dfx['total'].sum()
            if total != row['total']:
                print('Sum of total for camp {} not correct, {} given, expected {}'.format(current_camp, row['total'], total))
            
            day = dfx['day'].sum()
            if day != row['day']:
                print('Sum of total for camp {} not correct, {} given, expected {}'.format(current_camp, row['day'], day))
            
            overnight = dfx['overnight'].sum()
            if overnight != row['overnight']:
                print('Sum of total for camp {} not correct, {} given, expected {}'.format(current_camp, row['overnight'], overnight))
            rows_to_remove.append(i)
            continue
        
        # check if new camp name
        if math.isnan(row['total']) and math.isnan(row['day']) and math.isnan(row['overnight']):
            current_camp = row['hour']
            rows_to_remove.append(i)
            continue
        
        row_sum = row['day'] + row['overnight']
        if (row_sum != row['total']):
            print('Sum of day/overnight does not match total for camp {} at hour {}'.format(current_camp, row['hour']))
        
        df.at[i, 'camp'] = current_camp
        
    # drop meta rows
    df2 = df.drop(df.index[rows_to_remove]).reset_index(drop=True)


    # merge camp/gates
    # Liandi: For camps situated very close to a gate, add the numbers to the gate arrivals

    # CROCODILE BRIDGE REST CAMP – ADD the numbers to the CROCODILE BRIDGE GATE arrivals.
    dfx = df2[df2['camp'] == 'CROCODILE BRIDGE REST CAMP']
    for i, row in dfx.iterrows():
        
        loc = df2.loc[(df2['camp'] == 'CROCODILE BRIDGE GATE') & (df2['hour'] == row['hour'])]
        
        # add camp to gate for existing hours
        if not loc.empty:        
            df2.at[loc.index[0], 'total']     += row['total']
            df2.at[loc.index[0], 'day']       += row['day']
            df2.at[loc.index[0], 'overnight'] += row['overnight']
        else:
            # hour did not exist for gate, add it by overwrioting camp name with gate name ;)
            df2.at[i, 'camp'] = 'CROCODILE BRIDGE GATE'

    # ORPEN REST CAMP – ADD the numbers to the ORPEN GATE arrivals.
    dfx = df2[df2['camp'] == 'ORPEN REST CAMP']
    for i, row in dfx.iterrows():
        
        loc = df2.loc[(df2['camp'] == 'ORPEN GATE') & (df2['hour'] == row['hour'])]
        
        # add camp to gate for existing hours
        if not loc.empty:        
            df2.at[loc.index[0], 'total']     += row['total']
            df2.at[loc.index[0], 'day']       += row['day']
            df2.at[loc.index[0], 'overnight'] += row['overnight']
        else:
            # hour did not exist for gate, add it by overwrioting camp name with gate name ;)
            df2.at[i, 'camp'] = 'ORPEN GATE'
        

    # For all other rest camps, ignore the numbers. They are due to users in the system who, 
    # after helping visitors look for accommodation in the rest camp, forgot to change their 
    # location back to the gate. Excluding them will not make any material difference.

    # -> remove everything that is a CAMP
    df3 = df2[~df2.camp.str.contains('CAMP')]
    df3 = df3[~df3.camp.str.contains('AIRPORT')] # exclude aiurport aswell
    df3 = df3[~df3.camp.str.contains('LODGE')] # Lodges seem small <10 people...

    df3 = df3.reset_index(drop=True)

    # normalize hours
    df3['start_time'] = df3['hour'].apply(get_start_time)
    df3['end_time'] = df3['hour'].apply(get_end_time)

    name = "{}_{}.csv".format(str(df3.at[0, 'start_date']), str(df3.at[0, 'end_date']))
    df3.to_csv(name, index=False)