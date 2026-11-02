import os
from PIL import Image, ImageDraw

sprites_dir = r"D:\Webapp\RichyRich\RichRich_Unity\Assets\Sprites\UI"
os.makedirs(sprites_dir, exist_ok=True)

def create_9slice(filename, size, fill, border_color, border_width, radius):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Draw rounded rectangle
    draw.rounded_rectangle(
        [(0, 0), (size - 1, size - 1)],
        radius=radius,
        fill=fill,
        outline=border_color,
        width=border_width
    )
    img.save(os.path.join(sprites_dir, filename), "PNG")
    print(f"Generated {filename}")

# 1. Card Background 9-slice (64x64, 16px radius)
create_9slice("card_bg_9slice.png", 64, (21, 30, 46, 245), (42, 53, 72, 255), 2, 16)

# 2. Primary Button 9-slice (Emerald)
create_9slice("btn_primary_9slice.png", 64, (16, 185, 129, 255), (52, 211, 153, 220), 2, 14)

# 3. Secondary Dark Button 9-slice
create_9slice("btn_dark_9slice.png", 64, (30, 41, 59, 240), (51, 65, 85, 255), 2, 14)

# 4. Dock Background 9-slice
create_9slice("dock_bg_9slice.png", 64, (10, 15, 29, 250), (30, 41, 59, 255), 2, 16)

# 5. Destructive Button 9-slice (Crimson)
create_9slice("btn_danger_9slice.png", 64, (220, 38, 38, 255), (248, 113, 113, 220), 2, 14)

# 6. Icons (128x128 for crisp rendering on high-DPI mobile)
icons_dir = r"D:\Webapp\RichyRich\RichRich_Unity\Assets\Sprites\Icons"
os.makedirs(icons_dir, exist_ok=True)

def create_icon_city():
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Left building
    d.rectangle([(20, 45), (45, 110)], fill=(56, 189, 248, 255))
    # Center tower (tallest)
    d.rectangle([(48, 20), (80, 110)], fill=(245, 158, 11, 255))
    # Antenna
    d.line([(64, 10), (64, 20)], fill=(245, 158, 11, 255), width=3)
    # Right building
    d.rectangle([(83, 55), (108, 110)], fill=(16, 185, 129, 255))
    # Windows
    for y in range(30, 105, 14):
        d.rectangle([(56, y), (60, y+4)], fill=(15, 23, 42, 255))
        d.rectangle([(68, y), (72, y+4)], fill=(15, 23, 42, 255))
    for y in range(55, 105, 14):
        d.rectangle([(28, y), (37, y+4)], fill=(15, 23, 42, 255))
        d.rectangle([(91, y), (100, y+4)], fill=(15, 23, 42, 255))
    img.save(os.path.join(icons_dir, "icon_city.png"), "PNG")
    print("Generated icon_city.png")

def create_icon_career():
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Briefcase body
    d.rounded_rectangle([(22, 45), (106, 108)], radius=12, fill=(56, 189, 248, 255))
    # Handle
    d.arc([(46, 22), (82, 54)], start=180, end=0, fill=(56, 189, 248, 255), width=6)
    # Horizontal strap line
    d.line([(22, 70), (106, 70)], fill=(15, 23, 42, 255), width=4)
    # Center lock
    d.rectangle([(58, 65), (70, 78)], fill=(245, 158, 11, 255))
    img.save(os.path.join(icons_dir, "icon_career.png"), "PNG")
    print("Generated icon_career.png")

def create_icon_enterprise():
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Store base
    d.rounded_rectangle([(24, 48), (104, 112)], radius=8, fill=(16, 185, 129, 255))
    # Awning roof
    d.polygon([(16, 48), (64, 20), (112, 48)], fill=(245, 158, 11, 255))
    # Door
    d.rectangle([(50, 74), (78, 112)], fill=(15, 23, 42, 255))
    # Window panes
    d.rectangle([(32, 60), (46, 80)], fill=(248, 250, 252, 220))
    d.rectangle([(82, 60), (96, 80)], fill=(248, 250, 252, 220))
    img.save(os.path.join(icons_dir, "icon_enterprise.png"), "PNG")
    print("Generated icon_enterprise.png")

def create_icon_wealth():
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Diamond / Growth emblem
    d.polygon([(64, 16), (108, 48), (64, 112), (20, 48)], fill=(245, 158, 11, 255))
    # Facet lines
    d.line([(20, 48), (108, 48)], fill=(254, 243, 199, 255), width=3)
    d.line([(64, 16), (64, 112)], fill=(254, 243, 199, 255), width=3)
    d.line([(42, 48), (64, 112)], fill=(217, 119, 6, 255), width=2)
    d.line([(86, 48), (64, 112)], fill=(217, 119, 6, 255), width=2)
    img.save(os.path.join(icons_dir, "icon_wealth.png"), "PNG")
    print("Generated icon_wealth.png")

create_icon_city()
create_icon_career()
create_icon_enterprise()
create_icon_wealth()
print("All graphic assets generated successfully!")
