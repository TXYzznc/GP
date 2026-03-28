import pandas as pd
df = pd.read_excel('AAAGameData/DataTables/ResourceConfigTable.xlsx')
target_ids = [1701, 1702, 1703, 1704, 1705, 1706, 1707, 1708, 1801, 1802]
result = df[df['ID'].isin(target_ids)][['ID', 'Name', 'Path']]
print(result.to_string())
print('\n总行数:', len(df))
print('ID范围:', df['ID'].min(), '-', df['ID'].max())
