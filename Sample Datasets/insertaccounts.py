import pandas as pd 
import sqlite3
import numpy as np

target = [
"EmpId",
"NamePrefix",
"FirstName",
"MiddleInitial",
"LastName",
"Gender",
"Email",
"FathersName",
"MothersName",
"MothersMaidenName",
"DateOfBirth",
"TimeOfBirth",
"AgeInYears",
"WeightInKgs",
"DateOfJoining",
"QuarterOfJoining",
"HalfOfJoining",
"YearOfJoining",
"MonthOfJoining",
"MonthNameOfJoining",
"ShortMonth",
"DayOfJoining",
"DOWOfJoining",
"ShortDOW",
"AgeInCompanyYears",
"Salary",
"LastPercentHike",
"SSN",
"PhoneNumber",
"PlaceName",
"County",
"City",
"State",
"Zip",
"Region",
"UserName",
"Password"
]

current = [
"Emp ID",
"Name Prefix",
"First Name",
"Middle Initial",
"Last Name",
"Gender",
"E Mail",
"Father's Name",
"Mother's Name",
"Mother's Maiden Name",
"Date of Birth",
"Time of Birth",
"Age in Yrs.",
"Weight in Kgs.",
"Date of Joining",
"Quarter of Joining",
"Half of Joining",
"Year of Joining",
"Month of Joining",
"Month Name of Joining",
"Short Month",
"Day of Joining",
"DOW of Joining",
"Short DOW",
"Age in Company (Years)",
"Salary",
"Last % Hike",
"SSN",
"Phone No. ",
"Place Name",
"County",
"City",
"State",
"Zip",
"Region",
"User Name",
"Password"
]


temp = list(zip(current, target))
rename_dict = dict()
for item in temp:
    rename_dict[item[0]] = item[1]

df = pd.read_csv('10000 Records.csv')
print(rename_dict)
df = df.rename(columns=rename_dict)
print(df)

conn = sqlite3.connect('../demo.db')
df.to_sql('Accounts', conn, if_exists='append', index=False)
