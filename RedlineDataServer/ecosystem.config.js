module.exports = {
  /**
   * Application configuration section
   * http://pm2.keymetrics.io/docs/usage/application-declaration/
   */
  apps : [
    {
      name      : 'Redline Server',
      script    : './index.js',
      interpreter: 'node@8.9.2'
    }
  ]
};
