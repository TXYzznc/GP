import pandas as pd

df = pd.read_excel('D:/Sourcetree/Clash_Of_Gods/AAAGameData/DataTables/SummonerSkillTable.xlsx')
print('列数:', len(df.columns))
print('行数:', len(df))
print('\n列名:')
print(df.columns.tolist())
print('\n前3行数据:')
print(df.head(3).to_string())
print('\n第103行（ID=103）:')
print(df[df['ID'] == 103].to_string())
print('\n第105行（ID=105）:')
print(df[df['ID'] == 105].to_string())
