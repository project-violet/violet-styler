import json
import numpy as np
import matplotlib.pyplot as plt


with open('artpp.json') as json_file:
    json_data = json.load(json_file)

    x = []
    y = []

    for r in json_data:
        # if (r[0] > 250):
        #     continue
        x.append(r[0])
        y.append(r[1])

    
    plt.figure(figsize=(20, 10))
    plt.scatter(x, y)
    plt.savefig('savefig_default.png')