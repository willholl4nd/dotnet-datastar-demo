import pandas as pd 
import sqlite3
import numpy as np

df = pd.read_csv('baby-names.csv')
df = pd.DataFrame(df['name'])
df['RandInt'] = np.random.randint(0,100, size=len(df))
df.rename(columns={'name': 'Name'}, inplace=True)
df['DemoObjectId'] = 1
df['Id'] = df.index + 4

conn = sqlite3.connect('demo.db')
df.to_sql('Entries', conn, if_exists='append', index=False)
