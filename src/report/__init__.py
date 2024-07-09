import datetime
import json
import os
from confighelper import ConfigHelper
from logging import Logger


class Report:
    def __init__(self, start_date: datetime.date, end_date_exclusive: datetime.date, logger: Logger):
        self._confighelper = ConfigHelper()
        self._logger = logger

        self._product_names = self._load_product_names()

        self._start_date = start_date
        self._end_date_exclusive = end_date_exclusive
        self.report_items = self._filter_repo(self._load_repo())
        self.report = self._build_report()


    def _load_repo(self) -> list:
        products = []

        repo_releases_path = os.path.join(self._confighelper.get_repo_path(), 'releases')
        for product_file_name in sorted(os.listdir(repo_releases_path)):
            product_file_path = os.path.join(repo_releases_path, product_file_name)

            if not os.path.isfile(product_file_path):
                self._logger.debug(f"{product_file_name} is not a file")
                continue
            if not product_file_path.endswith('.json'):
                self._logger.debug(f"{product_file_name} is not a .json file")
                continue

            product_id, _ = os.path.splitext(product_file_name)

            product_releases = []
            try:
                with open(product_file_path) as file:
                    product_releases = json.load(file)['releases']
            except Exception as e:
                continue

            products.append(
            {
                'product_id': product_id,
                'product_name': self._get_product_name_or_default(product_id),
                'releases': product_releases
            })

        return products


    def _filter_repo(self, products: list) -> list:
        report_items = []
        for product in products:
            for release in product['releases'].values():
                if 'eol' in release:
                    # Some product releases have an end-of-life date defined as `true` (if the EOL date already passed)
                    # or `false` (if the EOL date has not yet been published).
                    # Ignore them.
                    if not isinstance(release['eol'], bool):
                        release_eol = datetime.datetime.strptime(release['eol'], '%Y-%m-%d').date()
                        if release_eol >= self._start_date and release_eol < self._end_date_exclusive:
                            report_items.append({
                                'product_id': product['product_id'],
                                'product_name': product['product_name'],
                                'release': release['name'],
                                'eol': release_eol
                            })
                            self._logger.debug(f"Added to report: {report_items[-1]}")

        return report_items


    def _build_report(self) -> str:
        report = f"End-of-life (EOL) calendar for the next {(self._end_date_exclusive - self._start_date).days} days:\n\n"
        current_date = self._start_date
        while current_date < self._end_date_exclusive:
            report += f"{current_date.strftime('%a, %d %B')}:\n"
            matching_items = [item for item in self.report_items if item['eol'] == current_date]
            if len(matching_items) > 0:
                for item in matching_items:
                    report += f"— <a href='https://endoflife.date/{item['product_id']}'><b>{item['product_name']} {item['release']}</b></a>\n"
            else:
                report += "None\n"
            report += "\n"
            current_date += datetime.timedelta(days=1)

        report += "<i>Source: https://github.com/endoflife-date/release-data</i>\n"
        return report


    def __str__(self):
        return self.report


    """
    Loads `products.json` that provides full product names.
    It should be periodically updated, for example, by running the following code in the browser console
    while staying on https://endoflife.date/:

    let jsonArray = [];
    document.querySelectorAll('.nav-list-item').forEach(item => {
    let anchor = item.querySelector('a');
    if(anchor) {
        let obj = {
        "id": anchor.getAttribute('href').slice(1),
        "name": anchor.textContent.trim()
        };
        jsonArray.push(obj);
    }
    });
    console.log(JSON.stringify(jsonArray, null, "\t"));
    """
    def _load_product_names(self) -> object:
        products_path = os.path.join(os.path.dirname(__file__), 'products.json')
        with open(products_path) as file:
            return json.load(file)


    """
    Returns the full name of the product from `products.json`.
    If the name is not found, it returns the product identifier (filename without '.json' in the release-data repository).
    """
    def _get_product_name_or_default(self, product_id) -> str:
        return next((item['name'] for item in self._product_names if item['id'] == product_id), product_id)
