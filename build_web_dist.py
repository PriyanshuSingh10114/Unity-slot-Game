import os
import shutil

def build_dist():
    dist_dir = "dist"
    if os.path.exists(dist_dir):
        shutil.rmtree(dist_dir)
    os.makedirs(dist_dir, exist_ok=True)

    # 1. Copy Assets needed for web game
    shutil.copytree("Assets/Art", os.path.join(dist_dir, "Assets/Art"), ignore=shutil.ignore_patterns("*.meta"))
    shutil.copytree("Assets/Sounds", os.path.join(dist_dir, "Assets/Sounds"), ignore=shutil.ignore_patterns("*.meta"))

    # 2. Read WebPreview/index.html and adapt paths for root hosting
    with open("WebPreview/index.html", "r", encoding="utf-8") as f:
        html = f.read()

    # In WebPreview, paths were "../Assets/". In dist, they are "./Assets/".
    html_dist = html.replace("../Assets/", "./Assets/")

    with open(os.path.join(dist_dir, "index.html"), "w", encoding="utf-8") as f:
        f.write(html_dist)

    print(f"Standalone deployment bundle built successfully in '{dist_dir}/'")

if __name__ == "__main__":
    build_dist()
