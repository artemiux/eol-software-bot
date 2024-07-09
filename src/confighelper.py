import os
import yaml


class ConfigHelper:

    def __init__(self):
        self.config = self._load()


    def get_base_dir(self) -> str:
        return os.path.join(os.path.dirname(__file__), '..')


    def get_db_path(self) -> str:
        db_path = self.config['db']['path']
        if not os.path.isabs(db_path):
            db_path = os.path.join(self.get_base_dir(), db_path)
        return db_path


    def get_log_path(self) -> str:
        log_path = self.config['logs']['path']
        if not os.path.isabs(log_path):
            log_path = os.path.join(self.get_base_dir(), log_path)
        return log_path


    def get_repo_path(self) -> str:
        repo_path = self.config['repo']['path']
        if not os.path.isabs(repo_path):
            repo_path = os.path.join(self.get_base_dir(), repo_path)
        return repo_path


    def _load(self) -> object:
        config_path = os.path.join(self.get_base_dir(), 'config.yaml')
        if not os.path.exists(config_path):
            raise FileNotFoundError('config.yaml not found')
        with open(config_path) as file:
            return yaml.load(file, Loader=yaml.FullLoader)
