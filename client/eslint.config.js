// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

const restrictImports = (...groups) => ({
  'no-restricted-imports': [
    'error',
    {
      patterns: groups.map((group) => ({
        group: [`@${group}/*`, `**/${group}/**`],
        message: `This layer must not import from ${group}/.`,
      })),
    },
  ],
});

module.exports = defineConfig([
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
    },
  },
  {
    files: ['src/app/features/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@features/*', '../*'],
              message:
                'A feature must not import another feature; reach core/, shared/ and layout/ through their @ aliases.',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['src/app/shared/**/*.ts'],
    rules: restrictImports('core', 'layout', 'features'),
  },
  {
    files: ['src/app/core/**/*.ts'],
    rules: restrictImports('layout', 'features'),
  },
  {
    files: ['src/app/layout/**/*.ts'],
    rules: restrictImports('features'),
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
]);
