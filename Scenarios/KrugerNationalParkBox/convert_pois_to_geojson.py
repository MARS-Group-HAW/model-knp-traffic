"""
    Generate GeoJSON from generated POI timings by `GetRouteTimings.cs`.
    Will consume `resources/pois.json` and create/overwrite `resources/pois.geojson`.

    See Jupyter notebook Prepare POI Layer for analyzing data.
"""

import json
import sys
from geojson import Point, Feature, FeatureCollection, dumps


try:
    with open('resources/pois_inferred.json') as f:
        data = json.load(f)
except EnvironmentError: # parent of IOError, OSError *and* WindowsError where available
    print("Make sure resources/pois.json exists and timings have been generated")
    sys.exit(1)


features = []

for origin in data:
    p = origin['Poi']['Position']

    props = {
        'name':      origin['Poi']['Name'],
        'type':      origin['Poi']['Type'],
        'access':    origin['Poi']['Access'],
        'routeList': origin
    }

    ft = Feature(geometry=Point((p['Longitude'], p['Latitude'])), properties=props)
    features.append(ft)

fc = FeatureCollection(features)
jsonString = dumps(fc)


with open("resources/pois_inferred.geojson", "w") as text_file:
    text_file.write(jsonString)
