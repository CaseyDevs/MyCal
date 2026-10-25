#!/usr/bin/env python3
"""Reproduce the bundled staples from the official USDA SR Legacy CSV download."""
import argparse
import csv
import hashlib
import io
import json
from pathlib import Path
from zipfile import ZipFile

SOURCE_URL = "https://fdc.nal.usda.gov/fdc-datasets/FoodData_Central_sr_legacy_food_csv_2018-04.zip"
SOURCE_SHA256 = "b80817294b8850530aaedf2e515c02593b1824f763a0ff356e5c2081643e6fd0"

# Friendly names preserve preparation and fat content; aliases only affect search.
SELECTION = [
    (171077, "Chicken breast, skinless, boneless, raw", "chicken breasts uncooked"),
    (171477, "Chicken breast, meat only, cooked, roasted", "chicken breasts skinless"),
    (171098, "Turkey breast, meat only, raw", "turkey breasts uncooked"),
    (171496, "Turkey breast, meat only, cooked, roasted", "turkey breasts"),
    (171790, "Beef mince, 5% fat, raw", "ground beef lean minced beef uncooked"),
    (171791, "Beef mince, 5% fat, cooked patty", "ground beef lean minced beef broiled"),
    (175167, "Salmon, Atlantic, farmed, raw", "fish uncooked"),
    (175168, "Salmon, Atlantic, farmed, cooked", "fish baked dry heat"),
    (171955, "Cod, Atlantic, raw", "fish uncooked"),
    (171956, "Cod, Atlantic, cooked", "fish baked dry heat"),
    (173709, "Tuna, canned in water, drained", "fish tinned"),
    (171287, "Egg, whole, raw", "eggs fresh"),
    (173424, "Egg, whole, hard-boiled", "eggs cooked boiled"),
    (172183, "Egg white, raw", "eggs whites fresh"),
    (173904, "Oats, dry", "porridge oats oatmeal rolled uncooked"),
    (173905, "Porridge oats, cooked with water, no salt", "oatmeal porridge oats"),
    (169756, "White rice, long-grain, dry", "rice raw uncooked"),
    (169757, "White rice, long-grain, cooked, no salt", "rice boiled"),
    (169703, "Brown rice, long-grain, dry", "rice raw uncooked wholegrain"),
    (169704, "Brown rice, long-grain, cooked", "rice boiled wholegrain"),
    (169736, "Pasta, enriched, dry", "pasta uncooked"),
    (169737, "Pasta, enriched, cooked, no salt", "pasta boiled"),
    (174924, "White bread, commercially prepared", "bread loaf"),
    (172688, "Wholemeal bread, commercially prepared", "whole wheat wholewheat wholegrain bread"),
    (171265, "Milk, whole, 3.25% fat", "milk dairy full fat"),
    (171267, "Milk, reduced fat, 2% fat", "milk dairy"),
    (171269, "Milk, skimmed, fat free", "milk dairy skim nonfat"),
    (170894, "Greek yoghurt, plain, fat free", "yogurt greek nonfat"),
    (170903, "Greek yoghurt, plain, low fat", "yogurt greek lowfat"),
    (171304, "Greek yoghurt, plain, whole milk", "yogurt greek full fat"),
    (173414, "Cheddar cheese", "cheese cheddar"),
    (172182, "Cottage cheese, 2% fat", "cheese cottage lowfat"),
    (172475, "Tofu, firm, raw, calcium-set", "tofu vegan"),
    (172421, "Lentils, boiled, no salt", "lentil cooked"),
    (173757, "Chickpeas, boiled, no salt", "chickpea garbanzo cooked"),
    (173735, "Black beans, boiled, no salt", "bean cooked"),
    (175194, "Red kidney beans, boiled, no salt", "bean cooked"),
    (171688, "Apple, raw, with skin", "apples fruit"),
    (173944, "Banana, raw", "bananas fruit"),
    (169097, "Orange, raw", "oranges fruit"),
    (167762, "Strawberries, raw", "strawberry fruit"),
    (171711, "Blueberries, raw", "blueberry fruit"),
    (171705, "Avocado, raw", "avocados fruit"),
    (170379, "Broccoli, raw", "vegetables"),
    (168462, "Spinach, raw", "vegetables"),
    (170393, "Carrots, raw", "carrot vegetables"),
    (168409, "Cucumber, raw, with peel", "cucumbers vegetables"),
    (170457, "Tomato, raw", "tomatoes vegetables"),
    (170000, "Onion, raw", "onions vegetables"),
    (170108, "Red pepper, sweet, raw", "peppers bell capsicum vegetables"),
    (169291, "Courgette, raw, with skin", "courgettes zucchini vegetables"),
    (169228, "Aubergine, raw", "aubergines eggplant vegetables"),
    (170026, "Potato, raw, with skin", "potatoes vegetables uncooked"),
    (170438, "Potato, boiled, flesh only, no salt", "potatoes cooked vegetables"),
    (170093, "Potato, baked, with skin, no salt", "potatoes jacket cooked vegetables"),
    (168482, "Sweet potato, raw", "sweet potatoes vegetables uncooked"),
    (171413, "Olive oil", "oil cooking"),
    (172336, "Rapeseed oil (canola)", "oil cooking"),
    (173410, "Butter, salted", "butter"),
    (173430, "Butter, unsalted", "butter without salt"),
    (170567, "Almonds", "almond nuts"),
    (170187, "Walnuts", "walnut nuts"),
    (172470, "Peanut butter, smooth, no salt", "peanut butter"),
]


def generate(archive):
    if hashlib.sha256(archive.read_bytes()).hexdigest() != SOURCE_SHA256:
        raise ValueError("The source archive differs from the reviewed USDA release.")
    selected = {str(code) for code, _, _ in SELECTION}
    with ZipFile(archive) as source:
        def rows(filename):
            path = next(n for n in source.namelist() if n.endswith("/" + filename))
            return csv.DictReader(io.TextIOWrapper(source.open(path), encoding="utf-8-sig"))

        descriptions = {r["fdc_id"]: r["description"] for r in rows("food.csv") if r["fdc_id"] in selected}
        nutrients = {code: {} for code in selected}
        units = {r["id"]: r["unit_name"].upper() for r in rows("nutrient.csv")}
        for row in rows("food_nutrient.csv"):
            code, nutrient = row["fdc_id"], row["nutrient_id"]
            if code in selected and nutrient in {"1008", "1003", "1004", "1005", "1079"}:
                expected_unit = "KCAL" if nutrient == "1008" else "G"
                if units[nutrient] != expected_unit:
                    raise ValueError(f"Unexpected unit for nutrient {nutrient}")
                nutrients[code][nutrient] = float(row["amount"]) if row["amount"] else None
    foods = []
    for code, name, aliases in SELECTION:
        values = nutrients[str(code)]
        if "1008" not in values:
            raise ValueError(f"Missing declared energy for {code}")
        foods.append(dict(FdcId=str(code), Name=name, SourceDescription=descriptions[str(code)], Aliases=aliases,
                          Calories=values["1008"], Protein=values.get("1003"), Fat=values.get("1004"),
                          TotalCarbohydrates=values.get("1005"), Fibre=values.get("1079")))
    header = dict(Title="USDA FoodData Central SR Legacy", Release="2018-04", DownloadUrl=SOURCE_URL,
                  ArchiveSha256=hashlib.sha256(archive.read_bytes()).hexdigest(), Licence="CC0-1.0")
    return ('{\n  "Source": ' + json.dumps(header) + ',\n  "Foods": [\n'
            + ',\n'.join('    ' + json.dumps(food, ensure_ascii=False) for food in foods) + '\n  ]\n}\n')


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path, help="Official USDA SR Legacy 2018-04 CSV ZIP")
    parser.add_argument("--check", action="store_true", help="Verify the checked-in snapshot without changing it")
    args = parser.parse_args()
    output = Path(__file__).resolve().parents[1] / "MyCal.Infrastructure/Nutrition/Data/generic-foods.json"
    content = generate(args.archive)
    if args.check:
        if output.read_text() != content:
            raise SystemExit("The bundled snapshot differs from the selected USDA source rows.")
        print(f"Verified {len(SELECTION)} source rows against the bundled snapshot.")
    else:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(content)
        print(f"Imported {len(SELECTION)} generic foods.")
