import logging
import logginghelper
import subprocess
from confighelper import ConfigHelper


"""
Updates the local branch of the `release-data` repository.
"""


def main():
    confighelper = ConfigHelper()

    logginghelper.setup_env(confighelper)
    logginghelper.setup_logging(confighelper, logger_name=__name__)
    logger = logging.getLogger(__name__)

    try:
        repo_path = confighelper.get_repo_path()
        checkout_result = subprocess.run(['git', 'checkout'], check=True, capture_output=True, cwd=repo_path)
        logger.info(checkout_result.stdout.decode().strip())
        pull_result = subprocess.run(['git', 'pull'], check=True, capture_output=True, cwd=repo_path)
        logger.info(pull_result.stdout.decode().strip())
    except Exception as e:
        logger.error(f"Exception while updating the release-data repository: {e}")


if __name__ == '__main__':
    main()
