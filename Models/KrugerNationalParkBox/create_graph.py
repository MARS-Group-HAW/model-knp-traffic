import osmnx as mars
from osmnx import io

G = mars.graph_from_place('Kruger National Park', which_result=2, network_type='drive', simplify=True)
#io.save_graph_shapefile(G,"kruger_drive_graph.shp")
io.save_graph_geopackage(G, "kruger_drive_graph.gpkg")

#G2 = mars.project_graph(mars.graph_from_place('Harburg, Hamburg, Germany', network_type='drive'))
#G3 = mars.consolidate_intersections(G2, tolerance=10, rebuild_graph=True, dead_ends=True)

#F = mars.pois_from_place('Harburg, Hamburg, Germany', tags={'amenity': ['parking_space']})
#F.to_file("Parking_Harburg", driver="ESRI Shapefile")
