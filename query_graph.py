import json, sys
import networkx as nx
from networkx.readwrite import json_graph
from pathlib import Path

data = json.loads(Path('graphify-out/graph.json').read_text())
G = json_graph.node_link_graph(data, edges='links')

question = '棋子 角色 英雄 战斗'
terms = [t.lower() for t in question.split() if len(t) > 1]

# Find best-matching start nodes
scored = []
for nid, ndata in G.nodes(data=True):
    label = ndata.get('label', '').lower()
    score = sum(1 for t in terms if t in label)
    if score > 0:
        scored.append((score, nid, ndata.get('label', nid)))

scored.sort(reverse=True)
start_nodes = [nid for _, nid, _ in scored[:8]]

if not start_nodes:
    print('未找到匹配的节点')
    sys.exit(0)

print('=== 匹配的关键节点 ===')
for idx, (_, nid, label) in enumerate(scored[:8]):
    print(f'{idx+1}. {label}')

subgraph_nodes = set()
subgraph_edges = []

# BFS
frontier = set(start_nodes)
subgraph_nodes = set(start_nodes)
for depth in range(3):
    next_frontier = set()
    for n in frontier:
        for neighbor in G.neighbors(n):
            if neighbor not in subgraph_nodes:
                next_frontier.add(neighbor)
                subgraph_edges.append((n, neighbor))
    subgraph_nodes.update(next_frontier)
    frontier = next_frontier

def relevance(nid):
    label = G.nodes[nid].get('label', '').lower()
    return sum(1 for t in terms if t in label)

ranked_nodes = sorted(list(subgraph_nodes), key=relevance, reverse=True)[:40]

print(f'\n=== 相关核心系统 ===')
for nid in ranked_nodes:
    d = G.nodes[nid]
    source_file = d.get('source_file', '').split('/')[-1] if d.get('source_file') else '?'
    print(f'  {d.get("label", nid)} ({source_file})')

print(f'\n=== 系统之间的关系 ===')
shown = 0
for u, v in subgraph_edges[:30]:
    if u in subgraph_nodes and v in subgraph_nodes and shown < 25:
        edge_d = G.edges[u, v]
        ulabel = G.nodes[u].get('label',u)
        vlabel = G.nodes[v].get('label',v)
        rel = edge_d.get('relation', '')
        conf = edge_d.get('confidence', '')
        print(f'  {ulabel} --{rel}--> {vlabel} [{conf}]')
        shown += 1
