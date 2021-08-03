import osmnx as mars
from osmnx import io

mars.config(log_console=True)

G = mars.graph_from_place('MP325', network_type='drive', simplify=True)
io.save_graphml(G, 'kruger_drive_graph.graphml')
# io.save_graph_shapefile(G,"kruger_drive_graph.shp")
# io.save_graph_geopackage(G, "kruger_drive_graph.gpkg")


